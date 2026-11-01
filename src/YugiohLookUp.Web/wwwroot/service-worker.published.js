// Keep each published app, dataset, and artwork together as one integrity-checked snapshot.
self.importScripts("./service-worker-assets.js");

const baseUrl = new URL("./", self.location.href);
const cachePrefix = `arcv-cache-${encodeURIComponent(baseUrl.pathname)}-`;
const cacheName = `${cachePrefix}${self.assetsManifest.version}`;
const assets = self.assetsManifest.assets.filter(asset =>
    !/(^|\/)service-worker(\.published)?\.js$/.test(asset.url) &&
    !/\.pdb$/.test(asset.url));
const urls = assets.map(asset => new URL(asset.url, baseUrl).href);

self.addEventListener("install", event => event.waitUntil(install()));
self.addEventListener("activate", event => event.waitUntil(activate()));
self.addEventListener("fetch", event => event.respondWith(fetchAsset(event.request)));
self.addEventListener("message", event => {
    if (event.data?.type === "CACHE_STATUS" && event.ports[0]) {
        event.waitUntil(checkCache().then(status => event.ports[0].postMessage(status)));
    }
    if (event.data?.type === "REPAIR_CACHE" && event.ports[0]) {
        event.waitUntil(repairCache().then(status => event.ports[0].postMessage(status)));
    }
});

async function populateCache() {
    const cache = await caches.open(cacheName);
    await cache.addAll(assets.map(asset => new Request(new URL(asset.url, baseUrl), {
        integrity: asset.hash,
        cache: "no-cache"
    })));
}

async function install() {
    try {
        await populateCache();
    } catch (error) {
        await caches.delete(cacheName);
        console.error("Offline snapshot installation failed; retaining the previous version.", error);
        throw error;
    }
}

async function repairCache() {
    try {
        await populateCache();
        return await checkCache();
    } catch (error) {
        console.error("Offline cache repair failed.", error);
        return { ready: false, failed: true, message: "Recaching failed. Stay online and retry, or close/reopen to apply a pending update." };
    }
}

async function activate() {
    const keys = await caches.keys();
    await Promise.all(keys.filter(key => key.startsWith(cachePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    await self.clients.claim();
}

async function fetchAsset(request) {
    const url = new URL(request.url);
    if (request.method !== "GET" || url.origin !== baseUrl.origin || !url.pathname.startsWith(baseUrl.pathname))
        return fetch(request);
    const navigation = request.mode === "navigate" && !urls.includes(request.url);
    const cache = await caches.open(cacheName);
    return await cache.match(navigation ? new URL("index.html", baseUrl).href : request) || fetch(request);
}

async function checkCache() {
    const cache = await caches.open(cacheName);
    const cached = new Set((await cache.keys()).map(request => request.url));
    const ready = urls.every(url => cached.has(url));
    return {
        ready,
        failed: !ready,
        message: ready
            ? "Offline cache ready. Searches and bundled artwork work without a connection."
            : "Offline files are missing. Connect to the internet and retry to recache."
    };
}

// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });
self.addEventListener('message', event => {
    if (['CACHE_STATUS', 'REPAIR_CACHE'].includes(event.data?.type) && event.ports[0])
        event.ports[0].postMessage({
            ready: false,
            failed: false,
            message: 'Development build: offline caching is enabled only after publishing.'
        });
});
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));

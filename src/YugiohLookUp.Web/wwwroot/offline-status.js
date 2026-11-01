let registration;
let installFailed = false;

export async function register() {
    if (!("serviceWorker" in navigator)) return null;
    if (!registration) {
        registration = navigator.serviceWorker.register("service-worker.js", { updateViaCache: "none" })
            .then(reg => {
                const observe = worker => {
                    if (!worker) return;
                    worker.addEventListener("statechange", () => {
                        if (worker.state === "redundant") installFailed = true;
                        if (worker.state === "activated") installFailed = false;
                    });
                };
                observe(reg.installing);
                reg.addEventListener("updatefound", () => observe(reg.installing));
                return reg;
            });
    }
    try {
        return await registration;
    } catch (error) {
        registration = null;
        throw error;
    }
}

export async function getStatus(repair = false) {
    const reg = await register();
    if (!reg) return { message: "This browser cannot cache the app offline.", failed: true };
    const worker = navigator.serviceWorker.controller;
    if (!worker) {
        if (repair) {
            installFailed = false;
            await reg.update();
        }
        return {
            message: installFailed
                ? "Initial offline caching failed. Stay online, reload, and retry."
                : "Preparing offline files. Stay online until the cache is ready.",
            failed: installFailed
        };
    }
    const status = await new Promise((resolve, reject) => {
        const channel = new MessageChannel();
        const timeout = setTimeout(() => {
            channel.port1.close();
            reject(new Error("Service worker did not respond to the cache check."));
        }, repair ? 120000 : 5000);
        channel.port1.onmessage = event => {
            clearTimeout(timeout);
            channel.port1.close();
            resolve(event.data);
        };
        worker.postMessage({ type: repair ? "REPAIR_CACHE" : "CACHE_STATUS" }, [channel.port2]);
    });
    if (repair && !status.ready) {
        try { await reg.update(); }
        catch (error) {
            console.error("Update check after cache repair failed.", error);
            status.message += " The update check also failed; check your connection.";
        }
    }
    if (status.ready && reg.waiting)
        return { message: "Offline cache ready. Update available: close all app tabs/windows and reopen to update.", failed: false };
    if (status.ready && installFailed)
        return { message: "Offline cache ready. The update failed; your previous version is retained. Retry by reopening online.", failed: false };
    return { message: status.message, failed: status.failed };
}

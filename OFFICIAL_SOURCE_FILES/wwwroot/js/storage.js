const dbName = 'SandbloxDB';
const storeName = 'worlds';
let db;

async function openDB() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, 1);
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            db.createObjectStore(storeName);
        };
    });
}

export async function saveWorld(data) {
    if (!db) db = await openDB();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readwrite');
        const store = tx.objectStore(storeName);
        const request = store.put(data, 'currentWorld');
        request.onsuccess = () => resolve();
        request.onerror = () => reject(request.error);
    });
}

export async function loadWorld() {
    if (!db) db = await openDB();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(storeName, 'readonly');
        const store = tx.objectStore(storeName);
        const request = store.get('currentWorld');
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}
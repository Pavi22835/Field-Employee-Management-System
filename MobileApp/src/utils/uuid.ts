// The `uuid` package's v4() calls crypto.getRandomValues(), which Hermes does not provide
// on Android without an extra native polyfill (react-native-get-random-values). Neither of
// this app's two uuid uses is security-sensitive (a device install identifier, and a
// fallback file name), so a Math.random()-based v4 generator avoids the crash without
// adding a new native dependency.
export function generateUuidV4(): string {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === "x" ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

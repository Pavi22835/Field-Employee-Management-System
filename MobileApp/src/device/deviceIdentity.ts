import * as SecureStore from "expo-secure-store";
import * as Application from "expo-application";
import * as Device from "expo-device";
import { generateUuidV4 } from "@/utils/uuid";

// Section 6: device identity is derived from an app-scoped GUID generated on first
// launch and stored in encrypted storage (Android Keystore, via expo-secure-store) —
// deliberately NOT IMEI, which modern Android restricts app access to.
const INSTALL_ID_KEY = "fems.appInstallationId";

export async function getOrCreateInstallationId(): Promise<string> {
  const existing = await SecureStore.getItemAsync(INSTALL_ID_KEY);
  if (existing) return existing;

  const generated = generateUuidV4();
  await SecureStore.setItemAsync(INSTALL_ID_KEY, generated);
  return generated;
}

export async function getDeviceEnrollmentPayload() {
  return {
    appInstallationId: await getOrCreateInstallationId(),
    androidId: Application.getAndroidId() ?? undefined,
    manufacturer: Device.manufacturer ?? undefined,
    model: Device.modelName ?? undefined,
    osVersion: Device.osVersion ?? undefined,
    appVersion: Application.nativeApplicationVersion ?? undefined,
    pushToken: undefined as string | undefined // populated by the notifications module once FCM/Expo push registers
  };
}

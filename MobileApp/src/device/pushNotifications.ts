import * as Notifications from "expo-notifications";
import { Platform } from "react-native";

// Section 20: FCM push token registration. In Expo Go, getDevicePushTokenAsync only
// returns a usable FCM token on a custom dev client / EAS build with google-services.json
// configured — see MobileApp/README.md for the production setup note.
export async function getOrRegisterPushToken(): Promise<string | null> {
  if (Platform.OS !== "android") return null;

  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;

  if (existingStatus !== "granted") {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  if (finalStatus !== "granted") return null;

  try {
    const token = await Notifications.getDevicePushTokenAsync();
    return token.data;
  } catch {
    return null;
  }
}

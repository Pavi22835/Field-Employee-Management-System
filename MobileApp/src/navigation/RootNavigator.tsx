import { NavigationContainer } from "@react-navigation/native";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { ActivityIndicator, View } from "react-native";
import { useAuth } from "@/auth/AuthContext";
import { LoginScreen } from "@/screens/LoginScreen";
import { DashboardScreen } from "@/screens/DashboardScreen";
import { VisitDetailScreen } from "@/screens/VisitDetailScreen";
import { DynamicFormScreen } from "@/screens/DynamicFormScreen";
import { VisitCompleteScreen } from "@/screens/VisitCompleteScreen";

const Stack = createNativeStackNavigator();

export function RootNavigator() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <View style={{ flex: 1, justifyContent: "center" }}><ActivityIndicator size="large" /></View>;
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerStyle: { backgroundColor: "#1565C0" }, headerTintColor: "#fff" }}>
        {!isAuthenticated ? (
          <Stack.Screen name="Login" component={LoginScreen} options={{ headerShown: false }} />
        ) : (
          <>
            <Stack.Screen name="Dashboard" component={DashboardScreen} options={{ title: "FEMS Field App" }} />
            <Stack.Screen name="VisitDetail" component={VisitDetailScreen} options={{ title: "Field Visit" }} />
            <Stack.Screen name="DynamicForm" component={DynamicFormScreen} options={{ title: "Field Information" }} />
            <Stack.Screen name="VisitComplete" component={VisitCompleteScreen} options={{ title: "Complete Visit" }} />
          </>
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

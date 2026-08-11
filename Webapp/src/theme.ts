import { createTheme } from "@mui/material/styles";

// Material UI enterprise theme (section 5.2).
export const theme = createTheme({
  palette: {
    mode: "light",
    primary: { main: "#1565C0" },
    secondary: { main: "#2E7D32" },
    background: { default: "#F4F6F8" }
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: ['"Inter"', "Roboto", "Helvetica", "Arial", "sans-serif"].join(",")
  }
});

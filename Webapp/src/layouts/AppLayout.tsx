import { useState } from "react";
import {
  AppBar, Box, Drawer, IconButton, List, ListItemButton, ListItemIcon, ListItemText,
  Toolbar, Typography, Avatar, Menu, MenuItem
} from "@mui/material";
import {
  Menu as MenuIcon,
  LayoutDashboard,
  Users,
  Smartphone,
  Map,
  ClipboardList,
  BellRing,
  Settings
} from "lucide-react";
import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "@/auth/AuthContext";

const drawerWidth = 240;

const navItems = [
  { label: "Dashboard", path: "/", icon: <LayoutDashboard /> },
  { label: "Employees", path: "/employees", icon: <Users /> },
  { label: "Devices", path: "/devices", icon: <Smartphone /> },
  { label: "Field Areas", path: "/field-areas", icon: <Map /> },
  { label: "Assignments", path: "/assignments", icon: <ClipboardList /> },
  { label: "Alerts", path: "/alerts", icon: <BellRing /> },
  { label: "Settings", path: "/settings", icon: <Settings /> }
];

export function AppLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const { user, logout } = useAuth();

  const drawer = (
    <List>
      {navItems.map((item) => (
        <ListItemButton
          key={item.path}
          component={NavLink}
          to={item.path}
          sx={{ "&.active": { bgcolor: "action.selected" } }}
        >
          <ListItemIcon>{item.icon}</ListItemIcon>
          <ListItemText primary={item.label} />
        </ListItemButton>
      ))}
    </List>
  );

  return (
    <Box sx={{ display: "flex" }}>
      <AppBar position="fixed" sx={{ zIndex: (t) => t.zIndex.drawer + 1 }}>
        <Toolbar>
          <IconButton color="inherit" edge="start" onClick={() => setMobileOpen(!mobileOpen)} sx={{ mr: 2, display: { sm: "none" } }}>
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap sx={{ flexGrow: 1 }}>
            Field Employee Management System
          </Typography>
          <IconButton onClick={(e) => setAnchorEl(e.currentTarget)}>
            <Avatar sx={{ width: 32, height: 32 }}>{user?.username?.[0]?.toUpperCase()}</Avatar>
          </IconButton>
          <Menu anchorEl={anchorEl} open={!!anchorEl} onClose={() => setAnchorEl(null)}>
            <MenuItem disabled>{user?.username} ({user?.roles.join(", ")})</MenuItem>
            <MenuItem onClick={logout}>Logout</MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={() => setMobileOpen(false)}
        sx={{ display: { xs: "block", sm: "none" }, "& .MuiDrawer-paper": { width: drawerWidth } }}
      >
        {drawer}
      </Drawer>
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: "none", sm: "block" },
          width: { sm: drawerWidth },
          flexShrink: { sm: 0 },
          "& .MuiDrawer-paper": { width: drawerWidth, boxSizing: "border-box" }
        }}
        open
      >
        <Toolbar />
        {drawer}
      </Drawer>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          maxWidth: "100%",
          overflowX: "hidden"
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}

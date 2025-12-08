import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Navbar from "./Navbar";
import Login from "./Login";
import Play from "./Play";
import AdminDashboard from "./admin/AdminDashboard";
import { useAuth } from "../auth/AuthContext";
import { JSX } from "react";
import { getToken } from "@lib/http";

function RequireAuth({
                         role,
                         children,
                     }: {
    role?: "admin" | "player";
    children: JSX.Element;
}) {
    const { user } = useAuth();
    const token = getToken();
    
    if (!user && token) return null;

    if (!user) return <Navigate to="/login" replace />;

    if (role && user.role !== role) {
        return <Navigate to={user.role === "admin" ? "/admin" : "/Play"} replace />;
    }

    return children;
}

function HomeRedirect() {
    const { user } = useAuth();
    if (user) return <Navigate to={user.role === "admin" ? "/admin" : "/Play"} replace />;
    return <Navigate to="/login" replace />;
}

export default function App() {
    return (
        <BrowserRouter>
            <Navbar />
            <Routes>
                {}
                <Route path="/" element={<HomeRedirect />} />

                {}
                <Route path="/login" element={<Login />} />

                {}
                <Route
                    path="/Play"
                    element={
                        <RequireAuth role="player">
                            <Play />
                        </RequireAuth>
                    }
                />

                {}
                <Route
                    path="/admin"
                    element={
                        <RequireAuth role="admin">
                            <AdminDashboard />
                        </RequireAuth>
                    }
                />

                {}
                <Route path="*" element={<Navigate to="/login" replace />} />
            </Routes>
        </BrowserRouter>
    );
}

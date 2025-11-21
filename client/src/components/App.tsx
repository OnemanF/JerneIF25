import { createBrowserRouter, RouterProvider } from "react-router-dom";
import { DevTools } from "jotai-devtools";
import "jotai-devtools/styles.css";
import { Toaster } from "react-hot-toast";

function Dashboard() {
    return (
        <div style={{ padding: 24 }}>
            <h1>Jerne IF – Dead Pigeons</h1>
            <p>Welcome! Build your pages here.</p>
        </div>
    );
}

function NotFound() {
    return (
        <div style={{ padding: 24 }}>
            <h2>404</h2>
            <p>Page not found.</p>
        </div>
    );
}

const router = createBrowserRouter([
    { path: "/", element: <Dashboard /> },
    { path: "*", element: <NotFound /> },
]);

export default function App() {
    return (
        <>
            <RouterProvider router={router} />
            <DevTools />
            <Toaster position="top-center" reverseOrder={false} />
        </>
    );
}

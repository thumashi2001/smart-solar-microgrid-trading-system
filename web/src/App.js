import { BrowserRouter, Routes, Route } from "react-router-dom";

import Login from "./pages/Login";
import Users from "./pages/Users";
import Prosumers from "./pages/Prosumers";
import MicrogridNodes from "./pages/MicrogridNodes";

import Layout from "./components/Layout";
import ProtectedRoute from "./components/ProtectedRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Login */}
        <Route path="/" element={<Login />} />

        {/* User Management */}
        <Route
          path="/users"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <Users />
              </Layout>
            </ProtectedRoute>
          }
        />

        {/* Prosumer Management */}
        <Route
          path="/prosumers"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <Prosumers />
              </Layout>
            </ProtectedRoute>
          }
        />

        {/* Microgrid Node Management */}
        <Route
          path="/microgrid-nodes"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <MicrogridNodes />
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
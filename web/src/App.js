import { BrowserRouter, Routes, Route } from "react-router-dom";

import Login from "./pages/Login";
import Users from "./pages/Users";
import Prosumers from "./pages/Prosumers";
import MicrogridNodes from "./pages/MicrogridNodes";
import AddMicrogridNode from "./pages/AddMicrogridNode";
import MicrogridNodeDetails from "./pages/MicrogridNodeDetails";
import EditMicrogridNode from "./pages/EditMicrogridNode";
import BackofficeDashboard from "./pages/BackofficeDashboard";
import EnergySlots from "./pages/EnergySlots";
import ReservationManagement from "./pages/ReservationManagement";

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
        <Route
          path="/microgrid-nodes/:id"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <MicrogridNodeDetails />
              </Layout>
            </ProtectedRoute>
          }
        />

        <Route
  path="/microgrid-nodes/:id/edit"
  element={
    <ProtectedRoute allowedRoles={["Backoffice"]}>
      <Layout>
        <EditMicrogridNode />
      </Layout>
    </ProtectedRoute>
  }
/>

        <Route
  path="/microgrid-nodes/add"
  element={
    <ProtectedRoute allowedRoles={["Backoffice"]}>
      <Layout>
        <AddMicrogridNode />
      </Layout>
    </ProtectedRoute>
  }
/>

     <Route
  path="/dashboard"
  element={
    <ProtectedRoute allowedRoles={["Backoffice"]}>
      <Layout>
        <BackofficeDashboard />
      </Layout>
    </ProtectedRoute>
  }
/>

        {/* Component 2 — Energy Booking Slot Management */}
        <Route
          path="/energy-slots"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <EnergySlots />
              </Layout>
            </ProtectedRoute>
          }
        />

        {/* Component 2 — Reservation Monitoring (Backoffice) */}
        <Route
          path="/reservations"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout>
                <ReservationManagement />
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
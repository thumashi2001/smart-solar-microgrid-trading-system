import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./pages/Login";
import Users from "./pages/Users";
import Prosumers from "./pages/Prosumers";
import Layout from "./components/Layout";
import ProtectedRoute from "./components/ProtectedRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route
          path="/users"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout><Users /></Layout>
            </ProtectedRoute>
          }
        />
        <Route
          path="/prosumers"
          element={
            <ProtectedRoute allowedRoles={["Backoffice"]}>
              <Layout><Prosumers /></Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
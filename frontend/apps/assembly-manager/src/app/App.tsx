import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from '../features/login/LoginPage';
import { ProductPage } from '../features/products/ProductPage';
import { WorkstationPage } from '../features/workstations/WorkstationPage';
import { LinePage } from '../features/lines/LinePage';
import { LineDetailPage } from '../features/lines/LineDetailPage';
import { ProtectedRoute } from '../shared/auth/ProtectedRoute';
import { Layout } from './Layout';

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/products" replace />} />
          <Route path="/products" element={<ProductPage />} />
          <Route path="/workstations" element={<WorkstationPage />} />
          <Route path="/lines" element={<LinePage />} />
          <Route path="/lines/:id" element={<LineDetailPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

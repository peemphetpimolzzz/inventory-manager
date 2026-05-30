import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Categories } from './pages/Categories';
import { Dashboard } from './pages/Dashboard';
import { Products } from './pages/Products';

export function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/products" element={<Products />} />
        <Route path="/categories" element={<Categories />} />
      </Route>
    </Routes>
  );
}

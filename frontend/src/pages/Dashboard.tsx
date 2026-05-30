import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { formatCurrency, formatDate } from '../format';
import type { Dashboard as DashboardData } from '../types';

export function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<DashboardData>('/dashboard').then(setData).catch((e) => setError((e as Error).message));
  }, []);

  if (error) {
    return <div className="error-banner">{error}</div>;
  }
  if (!data) {
    return <div className="loading">Loading…</div>;
  }

  return (
    <div>
      <h1>Dashboard</h1>

      <div className="kpi-grid">
        <KpiCard label="Products" value={data.totalProducts} />
        <KpiCard label="Categories" value={data.totalCategories} />
        <KpiCard label="Stock value" value={formatCurrency(data.totalStockValue)} />
        <KpiCard
          label="Low stock"
          value={data.lowStockCount}
          warn={data.lowStockCount > 0}
        />
      </div>

      <section className="card">
        <h2>Recent activity</h2>
        <table className="data-table">
          <thead>
            <tr>
              <th>Product</th>
              <th>Type</th>
              <th className="num">Qty</th>
              <th>When</th>
            </tr>
          </thead>
          <tbody>
            {data.recentTransactions.map((t) => (
              <tr key={t.id}>
                <td>{t.productName}</td>
                <td>{t.type}</td>
                <td className="num">{t.quantity}</td>
                <td>{formatDate(t.createdAt)}</td>
              </tr>
            ))}
            {data.recentTransactions.length === 0 && (
              <tr>
                <td colSpan={4} className="empty">
                  No activity yet
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </section>
    </div>
  );
}

function KpiCard({ label, value, warn }: { label: string; value: number | string; warn?: boolean }) {
  return (
    <div className={warn ? 'kpi-card warn' : 'kpi-card'}>
      <div className="kpi-value">{value}</div>
      <div className="kpi-label">{label}</div>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { Badge } from '../components/Badge';
import { formatCurrency, formatDate } from '../format';
import type { Dashboard as DashboardData } from '../types';

export function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<DashboardData>('/dashboard')
      .then(setData)
      .catch((e) => setError((e as Error).message));
  }, []);

  if (error) {
    return <div className="error-banner">{error}</div>;
  }
  if (!data) {
    return <div className="loading">Loading…</div>;
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <div className="page-subtitle">
            Real-time stock valuation, low-stock replenishment alerts, and immutable movement ledger
          </div>
        </div>
        <Link to="/products" className="button primary" style={{ textDecoration: 'none' }}>
          Manage Products &amp; Stock →
        </Link>
      </div>

      <div className="kpi-grid">
        <KpiCard
          label="Products"
          value={data.totalProducts}
          badge="Active SKUs"
          foot="Tracked with real-time reorder thresholds"
        />
        <KpiCard
          label="Categories"
          value={data.totalCategories}
          badge="Taxonomy"
          foot="Structured catalog groups"
        />
        <KpiCard
          label="Stock value"
          value={formatCurrency(data.totalStockValue)}
          badge="+ACID Verified"
          foot="Live valuation across all on-hand units"
        />
        <KpiCard
          label="Low stock"
          value={data.lowStockCount}
          warn={data.lowStockCount > 0}
          badge={data.lowStockCount > 0 ? 'Reorder Req' : 'All Healthy'}
          foot={
            data.lowStockCount > 0
              ? 'At or below minimum reorder level'
              : 'Every SKU is above reorder threshold'
          }
        />
      </div>

      <div className="dashboard-split">
        <section className="card card-flush">
          <div className="card-header-bar">
            <h2 style={{ margin: 0 }}>Recent activity</h2>
            <span className="badge badge-neutral">Immutable Ledger</span>
          </div>
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
                  <td style={{ fontWeight: 600 }}>{t.productName}</td>
                  <td>
                    <Badge tone={t.type.toUpperCase().includes('IN') ? 'ok' : 'low'}>
                      {t.type}
                    </Badge>
                  </td>
                  <td className="num mono">
                    {t.type.toUpperCase().includes('IN') ? `+${t.quantity}` : `-${t.quantity}`}
                  </td>
                  <td style={{ color: 'var(--muted)' }}>{formatDate(t.createdAt)}</td>
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

        <aside className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h2 style={{ margin: 0 }}>Replenishment &amp; PO Guard</h2>
            <Badge tone={data.lowStockCount > 0 ? 'low' : 'ok'}>
              {data.lowStockCount > 0 ? `${data.lowStockCount} Low SKUs` : 'Optimal'}
            </Badge>
          </div>
          <p style={{ fontSize: 13, color: 'var(--text-secondary)', lineHeight: 1.5, marginTop: 0 }}>
            Every stock-in and stock-out movement is committed atomically with the product balance in
            a single SQL Server transaction. Movements that would drive stock below zero are rejected
            with HTTP 400.
          </p>
          <div
            style={{
              background: 'var(--bg)',
              border: '1px solid var(--border)',
              borderRadius: 'var(--radius-sm)',
              padding: 12,
              fontSize: 12.5,
              display: 'flex',
              flexDirection: 'column',
              gap: 8,
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--muted)' }}>Healthy Inventory Ratio</span>
              <strong className="tabular">
                {data.totalProducts > 0
                  ? `${Math.round(((data.totalProducts - data.lowStockCount) / data.totalProducts) * 100)}%`
                  : '100%'}
              </strong>
            </div>
            <div className="stock-track" style={{ width: '100%' }}>
              <div
                className={`stock-fill ${data.lowStockCount > 0 ? 'low' : 'ok'}`}
                style={{
                  width:
                    data.totalProducts > 0
                      ? `${Math.max(12, Math.round(((data.totalProducts - data.lowStockCount) / data.totalProducts) * 100))}%`
                      : '100%',
                }}
              />
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', paddingTop: 4 }}>
              <Link to="/products" style={{ fontSize: 12, fontWeight: 600, color: 'var(--brand)' }}>
                Review low-stock items →
              </Link>
              <Link to="/categories" style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
                Manage categories
              </Link>
            </div>
          </div>
        </aside>
      </div>
    </div>
  );
}

function KpiCard({
  label,
  value,
  warn,
  badge,
  foot,
}: {
  label: string;
  value: number | string;
  warn?: boolean;
  badge: string;
  foot: string;
}) {
  return (
    <div className={warn ? 'kpi-card warn' : 'kpi-card'}>
      <div className="kpi-head">
        <div className="kpi-label">{label}</div>
        <Badge tone={warn ? 'low' : 'ok'}>{badge}</Badge>
      </div>
      <div className="kpi-value">{value}</div>
      <div className="kpi-foot">{foot}</div>
    </div>
  );
}

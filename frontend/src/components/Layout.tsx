import { useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';

const links = [
  { to: '/dashboard', label: 'Dashboard', tag: 'Live' },
  { to: '/products', label: 'Products', tag: 'SKUs' },
  { to: '/categories', label: 'Categories', tag: ' Catalog' },
];

export function Layout() {
  const [lang, setLang] = useState<'EN' | 'TH'>('EN');
  const [dark, setDark] = useState(false);
  const location = useLocation();

  const toggleTheme = (isDark: boolean) => {
    setDark(isDark);
    document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
  };

  const sectionTitle =
    location.pathname.startsWith('/products')
      ? 'Products & SKU Ledger'
      : location.pathname.startsWith('/categories')
        ? 'Category Taxonomy'
        : 'Inventory Overview';

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div>
          <div className="brand-wrap">
            <div className="brand-mark">SF</div>
            <div>
              <div className="brand">
                StockFlow <span>ERP</span>
              </div>
              <div className="brand-subtitle">Inventory &amp; Stock Ledger</div>
            </div>
          </div>

          <div className="nav-section-title">
            {lang === 'EN' ? 'Operations' : 'ระบบจัดการคลังสินค้า'}
          </div>
          <nav>
            {links.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
              >
                <span className="nav-link-inner">{link.label}</span>
                <span className="badge badge-neutral" style={{ fontSize: 10.5, padding: '1px 6px' }}>
                  {link.tag}
                </span>
              </NavLink>
            ))}
          </nav>
        </div>

        <div className="sidebar-footer">
          <div className="status-box">
            <div className="status-box-head">
              <span>SQL Server ACID Ledger</span>
              <span className="status-dot" />
            </div>
            <div className="status-box-sub">
              Transactional stock guard active · Zero negative-inventory drift
            </div>
          </div>
        </div>
      </aside>

      <div className="main-column">
        <header className="top-header">
          <div className="breadcrumbs">
            <span>StockFlow</span>
            <span>/</span>
            <strong>{sectionTitle}</strong>
          </div>

          <div className="header-controls">
            <div className="pill-toggle" role="group" aria-label="Language switch">
              <button
                type="button"
                className={lang === 'EN' ? 'active' : ''}
                onClick={() => setLang('EN')}
              >
                EN
              </button>
              <button
                type="button"
                className={lang === 'TH' ? 'active' : ''}
                onClick={() => setLang('TH')}
              >
                TH ไทย
              </button>
            </div>
            <div className="pill-toggle" role="group" aria-label="Theme switch">
              <button
                type="button"
                className={!dark ? 'active' : ''}
                onClick={() => toggleTheme(false)}
              >
                Light
              </button>
              <button
                type="button"
                className={dark ? 'active' : ''}
                onClick={() => toggleTheme(true)}
              >
                Dark
              </button>
            </div>
          </div>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

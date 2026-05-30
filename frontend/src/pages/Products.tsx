import { FormEvent, useCallback, useEffect, useState } from 'react';
import { api } from '../api/client';
import { Badge } from '../components/Badge';
import { Modal } from '../components/Modal';
import { formatCurrency } from '../format';
import type { Category, Paged, Product } from '../types';

interface ProductFormState {
  sku: string;
  name: string;
  categoryId: number;
  unitPrice: number;
  quantityOnHand: number;
  reorderLevel: number;
}

const emptyForm: ProductFormState = {
  sku: '',
  name: '',
  categoryId: 0,
  unitPrice: 0,
  quantityOnHand: 0,
  reorderLevel: 0,
};

export function Products() {
  const [page, setPage] = useState<Paged<Product> | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [search, setSearch] = useState('');
  const [lowOnly, setLowOnly] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [editing, setEditing] = useState<Product | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<ProductFormState>(emptyForm);

  const [movement, setMovement] = useState<{ product: Product; type: 'in' | 'out' } | null>(null);

  const load = useCallback(async () => {
    try {
      const params = new URLSearchParams();
      if (search) {
        params.set('search', search);
      }
      if (lowOnly) {
        params.set('lowStockOnly', 'true');
      }
      params.set('pageSize', '100');
      setPage(await api.get<Paged<Product>>(`/products?${params.toString()}`));
    } catch (e) {
      setError((e as Error).message);
    }
  }, [search, lowOnly]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    api.get<Category[]>('/categories').then(setCategories).catch(() => undefined);
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ ...emptyForm, categoryId: categories[0]?.id ?? 0 });
    setShowForm(true);
  };

  const openEdit = (product: Product) => {
    setEditing(product);
    setForm({
      sku: product.sku,
      name: product.name,
      categoryId: product.categoryId,
      unitPrice: product.unitPrice,
      quantityOnHand: product.quantityOnHand,
      reorderLevel: product.reorderLevel,
    });
    setShowForm(true);
  };

  const submitForm = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      if (editing) {
        await api.put(`/products/${editing.id}`, {
          sku: form.sku,
          name: form.name,
          categoryId: form.categoryId,
          unitPrice: form.unitPrice,
          reorderLevel: form.reorderLevel,
        });
      } else {
        await api.post('/products', form);
      }
      setShowForm(false);
      await load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const remove = async (product: Product) => {
    if (!window.confirm(`Delete ${product.name}?`)) {
      return;
    }
    try {
      await api.del(`/products/${product.id}`);
      await load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const submitMovement = async (quantity: number, note: string) => {
    if (!movement) {
      return;
    }
    try {
      await api.post(`/products/${movement.product.id}/stock-${movement.type}`, { quantity, note });
      setMovement(null);
      await load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <div>
      <div className="page-header">
        <h1>Products</h1>
        <button className="button primary" onClick={openCreate}>
          + New product
        </button>
      </div>

      {error && (
        <div className="error-banner" onClick={() => setError(null)}>
          {error}
        </div>
      )}

      <div className="toolbar">
        <input
          className="input"
          style={{ maxWidth: 280 }}
          placeholder="Search name or SKU…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <label className="checkbox">
          <input type="checkbox" checked={lowOnly} onChange={(e) => setLowOnly(e.target.checked)} />
          Low stock only
        </label>
      </div>

      <div className="card">
        <table className="data-table">
          <thead>
            <tr>
              <th>SKU</th>
              <th>Name</th>
              <th>Category</th>
              <th className="num">Price</th>
              <th className="num">On hand</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {page?.items.map((product) => (
              <tr key={product.id}>
                <td className="mono">{product.sku}</td>
                <td>{product.name}</td>
                <td>{product.categoryName}</td>
                <td className="num">{formatCurrency(product.unitPrice)}</td>
                <td className="num">{product.quantityOnHand}</td>
                <td>
                  {product.isLowStock ? <Badge tone="low">Low</Badge> : <Badge tone="ok">OK</Badge>}
                </td>
                <td className="actions">
                  <button className="link-button" onClick={() => setMovement({ product, type: 'in' })}>
                    In
                  </button>
                  <button className="link-button" onClick={() => setMovement({ product, type: 'out' })}>
                    Out
                  </button>
                  <button className="link-button" onClick={() => openEdit(product)}>
                    Edit
                  </button>
                  <button className="link-button danger" onClick={() => remove(product)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
            {page && page.items.length === 0 && (
              <tr>
                <td colSpan={7} className="empty">
                  No products found
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {showForm && (
        <Modal title={editing ? 'Edit product' : 'New product'} onClose={() => setShowForm(false)}>
          <form className="form" onSubmit={submitForm}>
            <label>
              SKU
              <input
                className="input"
                required
                value={form.sku}
                onChange={(e) => setForm({ ...form, sku: e.target.value })}
              />
            </label>
            <label>
              Name
              <input
                className="input"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </label>
            <label>
              Category
              <select
                className="input"
                value={form.categoryId}
                onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })}
              >
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Unit price
              <input
                className="input"
                type="number"
                min="0"
                step="0.01"
                value={form.unitPrice}
                onChange={(e) => setForm({ ...form, unitPrice: Number(e.target.value) })}
              />
            </label>
            {!editing && (
              <label>
                Initial quantity
                <input
                  className="input"
                  type="number"
                  min="0"
                  value={form.quantityOnHand}
                  onChange={(e) => setForm({ ...form, quantityOnHand: Number(e.target.value) })}
                />
              </label>
            )}
            <label>
              Reorder level
              <input
                className="input"
                type="number"
                min="0"
                value={form.reorderLevel}
                onChange={(e) => setForm({ ...form, reorderLevel: Number(e.target.value) })}
              />
            </label>
            <div className="form-actions">
              <button type="button" className="button" onClick={() => setShowForm(false)}>
                Cancel
              </button>
              <button type="submit" className="button primary">
                Save
              </button>
            </div>
          </form>
        </Modal>
      )}

      {movement && (
        <MovementModal
          product={movement.product}
          type={movement.type}
          onClose={() => setMovement(null)}
          onSubmit={submitMovement}
        />
      )}
    </div>
  );
}

function MovementModal({
  product,
  type,
  onClose,
  onSubmit,
}: {
  product: Product;
  type: 'in' | 'out';
  onClose: () => void;
  onSubmit: (quantity: number, note: string) => void;
}) {
  const [quantity, setQuantity] = useState(1);
  const [note, setNote] = useState('');

  return (
    <Modal title={`Stock ${type === 'in' ? 'in' : 'out'} — ${product.name}`} onClose={onClose}>
      <form
        className="form"
        onSubmit={(event) => {
          event.preventDefault();
          onSubmit(quantity, note);
        }}
      >
        <p>
          On hand: <strong>{product.quantityOnHand}</strong>
        </p>
        <label>
          Quantity
          <input
            className="input"
            type="number"
            min="1"
            required
            value={quantity}
            onChange={(e) => setQuantity(Number(e.target.value))}
          />
        </label>
        <label>
          Note
          <input className="input" value={note} onChange={(e) => setNote(e.target.value)} />
        </label>
        <div className="form-actions">
          <button type="button" className="button" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="button primary">
            Confirm
          </button>
        </div>
      </form>
    </Modal>
  );
}

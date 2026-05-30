import { FormEvent, useEffect, useState } from 'react';
import { api } from '../api/client';
import { Modal } from '../components/Modal';
import type { Category } from '../types';

export function Categories() {
  const [items, setItems] = useState<Category[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');

  const load = () =>
    api.get<Category[]>('/categories').then(setItems).catch((e) => setError((e as Error).message));

  useEffect(() => {
    void load();
  }, []);

  const openCreate = () => {
    setEditing(null);
    setName('');
    setDescription('');
    setShowForm(true);
  };

  const openEdit = (category: Category) => {
    setEditing(category);
    setName(category.name);
    setDescription(category.description ?? '');
    setShowForm(true);
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      const body = { name, description: description || null };
      if (editing) {
        await api.put(`/categories/${editing.id}`, body);
      } else {
        await api.post('/categories', body);
      }
      setShowForm(false);
      await load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const remove = async (category: Category) => {
    if (!window.confirm(`Delete ${category.name}?`)) {
      return;
    }
    try {
      await api.del(`/categories/${category.id}`);
      await load();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  return (
    <div>
      <div className="page-header">
        <h1>Categories</h1>
        <button className="button primary" onClick={openCreate}>
          + New category
        </button>
      </div>

      {error && (
        <div className="error-banner" onClick={() => setError(null)}>
          {error}
        </div>
      )}

      <div className="card">
        <table className="data-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Description</th>
              <th className="num">Products</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {items.map((category) => (
              <tr key={category.id}>
                <td>{category.name}</td>
                <td>{category.description}</td>
                <td className="num">{category.productCount}</td>
                <td className="actions">
                  <button className="link-button" onClick={() => openEdit(category)}>
                    Edit
                  </button>
                  <button className="link-button danger" onClick={() => remove(category)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
            {items.length === 0 && (
              <tr>
                <td colSpan={4} className="empty">
                  No categories yet
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {showForm && (
        <Modal title={editing ? 'Edit category' : 'New category'} onClose={() => setShowForm(false)}>
          <form className="form" onSubmit={submit}>
            <label>
              Name
              <input className="input" required value={name} onChange={(e) => setName(e.target.value)} />
            </label>
            <label>
              Description
              <input
                className="input"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
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
    </div>
  );
}

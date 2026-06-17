import { FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getProducts } from '../../shared/api/productsApi';
import {
  AssemblyLine,
  AssemblyLineInput,
  createAssemblyLine,
  deleteAssemblyLine,
  getAssemblyLines,
  updateAssemblyLine
} from '../../shared/api/linesApi';

const emptyForm = { name: '', productId: '', active: true };

export function LinePage() {
  const queryClient = useQueryClient();
  const [filterProductId, setFilterProductId] = useState('');
  const [form, setForm] = useState<{ name: string; productId: string; active: boolean }>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);

  const productsQuery = useQuery({ queryKey: ['products'], queryFn: getProducts });
  const linesQuery = useQuery({
    queryKey: ['lines', filterProductId],
    queryFn: () => getAssemblyLines(filterProductId || undefined)
  });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['lines'] });
  const resetForm = () => {
    setForm(emptyForm);
    setEditingId(null);
  };

  const saveMutation = useMutation({
    mutationFn: () => {
      const input: AssemblyLineInput = {
        name: form.name.trim(),
        productId: form.productId,
        active: form.active
      };
      return editingId ? updateAssemblyLine(editingId, input) : createAssemblyLine(input);
    },
    onSuccess: () => {
      resetForm();
      refresh();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteAssemblyLine(id),
    onSuccess: () => {
      if (editingId) {
        resetForm();
      }
      refresh();
    }
  });

  function startEdit(line: AssemblyLine) {
    setEditingId(line.id);
    setForm({ name: line.name, productId: line.productId, active: line.active });
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    saveMutation.mutate();
  }

  const isValid = form.name.trim() && form.productId;
  const actionError = saveMutation.error ?? deleteMutation.error;

  return (
    <section className="panel">
      <div className="panel-head">
        <h1>Assembly lines</h1>
        <label className="filter">
          <span>Filter by product</span>
          <select value={filterProductId} onChange={(event) => setFilterProductId(event.target.value)}>
            <option value="">All products</option>
            {productsQuery.data?.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <form className="form grid-form" onSubmit={submit}>
        <input
          value={form.name}
          onChange={(event) => setForm({ ...form, name: event.target.value })}
          placeholder="Line name"
          aria-label="Line name"
        />
        <select
          value={form.productId}
          onChange={(event) => setForm({ ...form, productId: event.target.value })}
          aria-label="Product"
        >
          <option value="">Select product…</option>
          {productsQuery.data?.map((product) => (
            <option key={product.id} value={product.id}>
              {product.name}
            </option>
          ))}
        </select>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={form.active}
            onChange={(event) => setForm({ ...form, active: event.target.checked })}
          />
          <span>Active</span>
        </label>
        <div className="actions">
          <button type="submit" disabled={saveMutation.isPending || !isValid}>
            {editingId ? 'Save changes' : 'Add line'}
          </button>
          {editingId && (
            <button type="button" className="ghost" onClick={resetForm}>
              Cancel
            </button>
          )}
        </div>
      </form>

      {linesQuery.isLoading && <p>Loading assembly lines…</p>}
      {linesQuery.isError && <p className="error">{(linesQuery.error as Error).message}</p>}
      {actionError && <p className="error">{(actionError as Error).message}</p>}

      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Product</th>
            <th>Status</th>
            <th>Workstations</th>
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          {linesQuery.data?.map((line) => (
            <tr key={line.id} className={editingId === line.id ? 'row-editing' : undefined}>
              <td>{line.name}</td>
              <td>{line.productName}</td>
              <td>
                <span className={line.active ? 'pill pill-on' : 'pill pill-off'}>
                  {line.active ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td>{line.workstationCount}</td>
              <td className="actions">
                <Link className="button ghost" to={`/lines/${line.id}`}>
                  Manage
                </Link>
                <button type="button" className="ghost" onClick={() => startEdit(line)}>
                  Edit
                </button>
                <button type="button" className="danger" onClick={() => deleteMutation.mutate(line.id)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
          {linesQuery.data?.length === 0 && (
            <tr>
              <td colSpan={5} className="empty">
                No assembly lines{filterProductId ? ' for this product' : ''} yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  );
}

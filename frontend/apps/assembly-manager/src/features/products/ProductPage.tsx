import { FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createProduct,
  deleteProduct,
  getProducts,
  Product,
  updateProduct
} from '../../shared/api/productsApi';

export function ProductPage() {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const productsQuery = useQuery({ queryKey: ['products'], queryFn: getProducts });

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['products'] });

  const createMutation = useMutation({
    mutationFn: () => createProduct(name.trim()),
    onSuccess: () => {
      setName('');
      refresh();
    }
  });

  const renameMutation = useMutation({
    mutationFn: ({ product, nextName }: { product: Product; nextName: string }) =>
      updateProduct(product.id, nextName),
    onSuccess: refresh
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    onSuccess: refresh
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createMutation.mutate();
  }

  const actionError = createMutation.error ?? renameMutation.error ?? deleteMutation.error;

  return (
    <section className="panel">
      <div className="panel-head">
        <h1>Products</h1>
      </div>

      <form className="form" onSubmit={submit}>
        <input
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="New product name"
          aria-label="Product name"
        />
        <button type="submit" disabled={createMutation.isPending || name.trim().length === 0}>
          Add product
        </button>
      </form>

      {productsQuery.isLoading && <p>Loading products…</p>}
      {productsQuery.isError && <p className="error">{(productsQuery.error as Error).message}</p>}
      {actionError && <p className="error">{(actionError as Error).message}</p>}

      <table className="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Assembly lines</th>
            <th aria-label="Actions" />
          </tr>
        </thead>
        <tbody>
          {productsQuery.data?.map((product) => (
            <tr key={product.id}>
              <td>{product.name}</td>
              <td>{product.assemblyLineCount}</td>
              <td className="actions">
                <button
                  type="button"
                  className="ghost"
                  onClick={() => {
                    const nextName = window.prompt('Rename product', product.name);
                    if (nextName?.trim()) {
                      renameMutation.mutate({ product, nextName: nextName.trim() });
                    }
                  }}
                >
                  Rename
                </button>
                <button type="button" className="danger" onClick={() => deleteMutation.mutate(product.id)}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
          {productsQuery.data?.length === 0 && (
            <tr>
              <td colSpan={3} className="empty">
                No products yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </section>
  );
}

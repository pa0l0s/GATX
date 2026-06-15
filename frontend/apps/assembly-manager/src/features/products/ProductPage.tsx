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

  const refreshProducts = () => queryClient.invalidateQueries({ queryKey: ['products'] });

  const createMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      setName('');
      refreshProducts();
    }
  });

  const renameMutation = useMutation({
    mutationFn: ({ product, nextName }: { product: Product; nextName: string }) =>
      updateProduct(product.id, nextName),
    onSuccess: refreshProducts
  });

  const deleteMutation = useMutation({
    mutationFn: deleteProduct,
    onSuccess: refreshProducts
  });

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    createMutation.mutate(name);
  }

  return (
    <main className="page">
      <header className="header">
        <div>
          <p className="eyebrow">GATX learning demo</p>
          <h1>Assembly Line Manager</h1>
        </div>
        <span className="badge">React + .NET + PostgreSQL</span>
      </header>

      <section className="panel">
        <h2>Products</h2>
        <form className="form" onSubmit={submit}>
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            placeholder="Product name"
            aria-label="Product name"
          />
          <button type="submit" disabled={createMutation.isPending || name.trim().length === 0}>
            Add
          </button>
        </form>

        {productsQuery.isLoading && <p>Loading products...</p>}
        {productsQuery.isError && <p className="error">{productsQuery.error.message}</p>}
        {createMutation.isError && <p className="error">{createMutation.error.message}</p>}

        <div className="table">
          {productsQuery.data?.map((product) => (
            <article className="row" key={product.id}>
              <div>
                <strong>{product.name}</strong>
                <span>{product.assemblyLineCount} assembly lines</span>
              </div>
              <div className="actions">
                <button
                  type="button"
                  onClick={() => {
                    const nextName = window.prompt('Rename product', product.name);
                    if (nextName?.trim()) {
                      renameMutation.mutate({ product, nextName });
                    }
                  }}
                >
                  Rename
                </button>
                <button type="button" onClick={() => deleteMutation.mutate(product.id)}>
                  Delete
                </button>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

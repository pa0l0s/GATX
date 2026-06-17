import { apiFetch } from './apiClient';

export interface Product {
  id: string;
  name: string;
  assemblyLineCount: number;
}

export function getProducts(): Promise<Product[]> {
  return apiFetch<Product[]>('/api/products');
}

export function createProduct(name: string): Promise<Product> {
  return apiFetch<Product>('/api/products', {
    method: 'POST',
    body: JSON.stringify({ name })
  });
}

export function updateProduct(id: string, name: string): Promise<Product> {
  return apiFetch<Product>(`/api/products/${id}`, {
    method: 'PUT',
    body: JSON.stringify({ name })
  });
}

export function deleteProduct(id: string): Promise<void> {
  return apiFetch<void>(`/api/products/${id}`, { method: 'DELETE' });
}

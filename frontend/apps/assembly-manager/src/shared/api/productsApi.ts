export interface Product {
  id: string;
  name: string;
  assemblyLineCount: number;
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '';

export async function getProducts(): Promise<Product[]> {
  const response = await fetch(`${apiBaseUrl}/api/products`);
  return parseJson<Product[]>(response);
}

export async function createProduct(name: string): Promise<Product> {
  const response = await fetch(`${apiBaseUrl}/api/products`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name })
  });

  return parseJson<Product>(response);
}

export async function updateProduct(id: string, name: string): Promise<Product> {
  const response = await fetch(`${apiBaseUrl}/api/products/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name })
  });

  return parseJson<Product>(response);
}

export async function deleteProduct(id: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/products/${id}`, {
    method: 'DELETE'
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }
}

async function parseJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => undefined);
    throw new Error(problem?.title ?? `Request failed with status ${response.status}`);
  }

  return response.json() as Promise<T>;
}

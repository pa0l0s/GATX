import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { createRoot } from 'react-dom/client';
import { ProductPage } from './features/products/ProductPage';
import './styles.css';

const queryClient = new QueryClient();

createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ProductPage />
    </QueryClientProvider>
  </React.StrictMode>
);

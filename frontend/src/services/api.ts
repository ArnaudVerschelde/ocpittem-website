const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api';

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || `Request failed: ${res.status}`);
  }

  return res.json();
}

export interface CreateCheckoutRequest {
  name: string;
  email: string;
  quantity: number;
}

export interface CreateCheckoutResponse {
  checkoutUrl: string;
}

export interface ContactRequest {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export const api = {
  createTicketCheckout: (data: CreateCheckoutRequest) =>
    request<CreateCheckoutResponse>('/tickets/create-checkout', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  sendContactMessage: (data: ContactRequest) =>
    request<{ success: boolean }>('/contact', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
};

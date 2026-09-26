const API_BASE = import.meta.env.VITE_API_BASE_URL || '/api';

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });

  if (!res.ok) {
    const body = await res.text();
    let message = body || `Request failed: ${res.status}`;

    try {
      const parsed = JSON.parse(body) as { error?: string };
      if (parsed.error) message = parsed.error;
    } catch {
      // Keep the plain-text response.
    }

    throw new ApiError(message, res.status);
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

export interface CookieSaleProduct {
  id: 'coteDor' | 'lotus';
  label: string;
  unitPriceCents: number;
}

export interface CookieSaleConfig {
  enabled: boolean;
  eventDate: string;
  maximumTotalPackages: number;
  classes: string[];
  products: CookieSaleProduct[];
}

export interface CreateCookieSaleCheckoutRequest {
  name: string;
  email: string;
  className: string;
  coteDorQuantity: number;
  lotusQuantity: number;
}

export interface CookieSaleOrderStatus {
  paymentStatus: 'Pending' | 'Paid' | 'Failed' | 'Cancelled';
  confirmationNumber?: string;
}

export interface ContactRequest {
  name: string;
  email: string;
  subject: string;
  message: string;
}

export interface ValidateTicketResponse {
  valid: boolean;
  ticketId?: string;
  ticketType?: string;
  error?: string;
}

export type GalleryCategory = 'fotograaf' | 'photobooth' | 'sfeerbeelden';

export interface GalleryImage {
  name: string;
  category: GalleryCategory;
  originalUrl: string;
  thumbnailUrl: string;
}

export interface GalleryResponse {
  images: GalleryImage[];
}

export const api = {
  createTicketCheckout: (data: CreateCheckoutRequest) =>
    request<CreateCheckoutResponse>('/tickets/create-checkout', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  getCookieSaleConfig: () =>
    request<CookieSaleConfig>('/cookie-sale/config'),

  createCookieSaleCheckout: (data: CreateCookieSaleCheckoutRequest) =>
    request<CreateCheckoutResponse>('/cookie-sale/create-checkout', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  getCookieSaleOrderStatus: (sessionId: string) =>
    request<CookieSaleOrderStatus>(
      `/cookie-sale/order-status?session_id=${encodeURIComponent(sessionId)}`,
    ),

  sendContactMessage: (data: ContactRequest) =>
    request<{ success: boolean }>('/contact', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  validateTicket: (code: string) =>
    request<ValidateTicketResponse>(`/tickets/validate?code=${encodeURIComponent(code)}`),

  getBalParental2026Gallery: () =>
    request<GalleryResponse>('/gallery/bal-parental-2026'),
};

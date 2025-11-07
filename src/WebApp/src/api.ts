// простой клиент для локальной разработки; хосты жёстко заданы через localhost
const endpoints = {
  identity: 'http://localhost:5001',
  catalog: 'http://localhost:5002',
  order: 'http://localhost:5003'
}

export async function register(email: string, password: string) {
  const res = await fetch(`${endpoints.identity}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
  if (!res.ok && res.status !== 200 && res.status !== 204) {
    throw new Error('Register failed')
  }
}

export async function login(email: string, password: string): Promise<string> {
  const res = await fetch(`${endpoints.identity}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  })
  if (!res.ok) throw new Error('Login failed')
  const data = await res.json()
  return data.accessToken as string
}

export async function getProducts() {
  const res = await fetch(`${endpoints.catalog}/api/products`)
  if (!res.ok) throw new Error('Fetch products failed')
  return res.json()
}

export async function createOrder(customerId: string, item: { productId: string; quantity: number; unitPrice: number }) {
  const res = await fetch(`${endpoints.order}/api/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ customerId, items: [item] })
  })
  if (!res.ok) throw new Error('Create order failed')
  return res.json()
}

export async function createOrderTraced(traceId: string, customerId: string, item: { productId: string; quantity: number; unitPrice: number }) {
  const res = await fetch(`${endpoints.order}/api/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Trace-Id': traceId },
    body: JSON.stringify({ customerId, items: [item] })
  })
  if (!res.ok) throw new Error('Create order failed')
  return res.json()
}

export async function loginTraced(traceId: string, email: string, password: string) {
  const res = await fetch(`${endpoints.identity}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Trace-Id': traceId },
    body: JSON.stringify({ email, password })
  })
  if (!res.ok) throw new Error('Login failed')
  return res.json()
}

export async function registerTraced(traceId: string, email: string, password: string) {
  await fetch(`${endpoints.identity}/api/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Trace-Id': traceId },
    body: JSON.stringify({ email, password })
  })
}

export async function getProductsTraced(traceId: string) {
  const res = await fetch(`${endpoints.catalog}/api/products`, { headers: { 'X-Trace-Id': traceId } })
  if (!res.ok) throw new Error('Fetch products failed')
  return res.json()
}

export async function getTracesRecent() {
  // собираем события из всех сервисов; ошибки payment игнорируем (может быть недоступен)
  const [identity, catalog, order, payment] = await Promise.all([
    fetch(`${endpoints.identity}/api/trace`).then(r => r.json()),
    fetch(`${endpoints.catalog}/api/trace`).then(r => r.json()),
    fetch(`${endpoints.order}/api/trace`).then(r => r.json()),
    fetch(`${endpoints.payment}/api/trace`).then(r => r.json()).catch(() => [])
  ])
  return [...identity, ...catalog, ...order, ...payment]
    .sort((a: any, b: any) => new Date(a.timestampUtc).getTime() - new Date(b.timestampUtc).getTime())
}

export async function getTraceById(traceId: string) {
  const [identity, catalog, order, payment] = await Promise.all([
    fetch(`${endpoints.identity}/api/trace/${traceId}`).then(r => r.json()),
    fetch(`${endpoints.catalog}/api/trace/${traceId}`).then(r => r.json()),
    fetch(`${endpoints.order}/api/trace/${traceId}`).then(r => r.json()),
    fetch(`${endpoints.payment}/api/trace/${traceId}`).then(r => r.json()).catch(() => [])
  ])
  return [...identity, ...catalog, ...order, ...payment]
    .sort((a: any, b: any) => new Date(a.timestampUtc).getTime() - new Date(b.timestampUtc).getTime())
}

export async function createOrderMulti(customerId: string, items: { productId: string; quantity: number; unitPrice: number }[]) {
  const res = await fetch(`${endpoints.order}/api/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ customerId, items })
  })
  if (!res.ok) throw new Error('Create order failed')
  return res.json()
}



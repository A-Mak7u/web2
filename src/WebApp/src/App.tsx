import { useEffect, useMemo, useState } from 'react'
import {
  Box,
  Button,
  Card,
  CardBody,
  CardHeader,
  Center,
  Flex,
  Grid,
  GridItem,
  Heading,
  HStack,
  Icon,
  Input,
  VStack,
  SimpleGrid,
  Spinner,
  Stack,
  Text,
  Tooltip,
} from '@chakra-ui/react'
import { FiBox, FiCheckCircle, FiLock, FiMail, FiShoppingCart, FiShoppingBag } from 'react-icons/fi'
import { createOrder, createOrderMulti, getProducts, login, register } from './api'
import { Flow } from './Flow'

type Product = { id: string; name: string; price: number }

export function App() {
  const [view, setView] = useState<'shop' | 'flow'>('shop')
  const [email, setEmail] = useState('user@test.com')
  const [password, setPassword] = useState('P@ssw0rd!')
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'))
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(false)
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [banner, setBanner] = useState<{ text: string; status: 'success' | 'error' | 'warning' | 'info' } | null>(null)
  // корзина хранится в localStorage между перезапусками
  const [cart, setCart] = useState<{ product: Product; quantity: number }[]>(() => {
    try { return JSON.parse(localStorage.getItem('cart') || '[]') } catch { return [] }
  })

  useEffect(() => {
    getProducts()
      .then(setProducts)
      .catch(() => setBanner({ text: 'Не удалось получить товары', status: 'error' }))
      .finally(() => setCatalogLoading(false))
  }, [])

  const handleRegister = async () => {
    setLoading(true)
    try {
      await register(email, password)
      setBanner({ text: 'Пользователь зарегистрирован', status: 'success' })
    } catch {
      setBanner({ text: 'Ошибка регистрации (возможно пользователь уже существует)', status: 'warning' })
    } finally {
      setLoading(false)
    }
  }

  const handleLogin = async () => {
    setLoading(true)
    try {
      const t = await login(email, password)
      localStorage.setItem('token', t)
      setToken(t)
      setBanner({ text: 'Вход выполнен', status: 'success' })
    } catch {
      setBanner({ text: 'Не удалось войти', status: 'error' })
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    setToken(null)
    setBanner({ text: 'JWT очищен', status: 'info' })
  }

  const order = async (p: Product) => {
    setLoading(true)
    try {
      const customerId = crypto.randomUUID()
      const res = await createOrder(customerId, { productId: p.id, quantity: 1, unitPrice: p.price })
      setBanner({ text: `Заказ создан • ID: ${res.id} • Статус: ${res.status}`, status: 'success' })
    } catch {
      setBanner({ text: 'Не удалось создать заказ', status: 'error' })
    } finally {
      setLoading(false)
    }
  }

  const addToCart = (p: Product) => {
    const next = [...cart]
    const idx = next.findIndex(ci => ci.product.id === p.id)
    if (idx >= 0) next[idx].quantity += 1
    else next.push({ product: p, quantity: 1 })
    setCart(next)
    localStorage.setItem('cart', JSON.stringify(next))
    setBanner({ text: `${p.name} добавлен в корзину`, status: 'info' })
  }

  const clearCart = () => { setCart([]); localStorage.setItem('cart', '[]') }

  const checkoutCart = async () => {
    if (!cart.length) { setBanner({ text: 'Корзина пуста', status: 'warning' }); return }
    setLoading(true)
    try {
      const customerId = crypto.randomUUID()
      const items = cart.map(ci => ({ productId: ci.product.id, quantity: ci.quantity, unitPrice: ci.product.price }))
      const res = await createOrderMulti(customerId, items)
      setBanner({ text: `Заказ создан из корзины • ID: ${res.id} • Статус: ${res.status}`, status: 'success' })
      clearCart()
    } catch {
      setBanner({ text: 'Не удалось оформить корзину', status: 'error' })
    } finally {
      setLoading(false)
    }
  }

  // отображаем токен в усечённом виде, чтобы не засорять интерфейс
  const maskedToken = useMemo(() => (token ? `${token.slice(0, 8)} ... ${token.slice(-8)}` : '—'), [token])

  return (
    <Flex direction="column" minH="100vh" bgGradient="linear(to-br, gray.900, gray.800)">
      <Box as="header" px={10} py={6} borderBottom="1px solid" borderColor="whiteAlpha.200">
        <Flex align="center" justify="space-between">
          <HStack spacing={3}>
            <Center w={10} h={10} borderRadius="lg" bg="teal.500">
              <Icon as={FiShoppingCart} boxSize={5} />
            </Center>
            <Box>
              <Heading size="md">WebShop</Heading>
              <Text fontSize="sm" color="whiteAlpha.600">Store</Text>
            </Box>
          </HStack>
          <HStack spacing={3} fontSize="sm" color="whiteAlpha.700">
            <Button size="sm" variant={view==='shop'?'solid':'ghost'} onClick={() => setView('shop')}>Shop</Button>
            <Button size="sm" variant={view==='flow'?'solid':'ghost'} onClick={() => setView('flow')}>Flow</Button>
            <Button size="sm" variant="ghost" onClick={() => setView('shop')} leftIcon={<Icon as={FiShoppingCart}/>}>Cart ({cart.reduce((a,c)=>a+c.quantity,0)})</Button>
          </HStack>
        </Flex>
      </Box>

      <Box flex="1" px={{ base: 6, md: 10 }} py={8}>
        {view === 'flow' ? (
          <Flow />
        ) : (
        <SimpleGrid columns={{ base: 1, lg: 3 }} spacing={6} alignItems="stretch">
          <GridItem colSpan={{ base: 1, lg: 1 }}>
            <Card bg="whiteAlpha.100" border="1px solid" borderColor="whiteAlpha.200" backdropFilter="blur(14px)">
              <CardHeader>
                <Heading size="md">Авторизация</Heading>
                <Text color="whiteAlpha.700" fontSize="sm" mt={2}>
                  Регистрация и вход через IdentityService (JWT)
                </Text>
              </CardHeader>
              <CardBody>
                <Stack spacing={4}>
                  <HStack>
                    <Icon as={FiMail} color="whiteAlpha.600" />
                    <Input
                      value={email}
                      onChange={e => setEmail(e.target.value)}
                      placeholder="E-mail"
                      variant="filled"
                    />
                  </HStack>
                  <HStack>
                    <Icon as={FiLock} color="whiteAlpha.600" />
                    <Input
                      type="password"
                      value={password}
                      onChange={e => setPassword(e.target.value)}
                      placeholder="Пароль"
                      variant="filled"
                    />
                  </HStack>
                  <HStack spacing={3}>
                    <Button flex={1} onClick={handleRegister} isLoading={loading} variant="outline">
                      Регистрация
                    </Button>
                    <Button flex={1} onClick={handleLogin} isLoading={loading} colorScheme="teal">
                      Войти
                    </Button>
                  </HStack>
                  <Tooltip label={token ?? 'JWT ещё не получен'} placement="bottom-start" hasArrow>
                    <Flex align="center" justify="space-between" bg="whiteAlpha.100" px={3} py={2} borderRadius="md">
                      <HStack spacing={2}>
                        <Icon as={FiCheckCircle} color={token ? 'teal.300' : 'whiteAlpha.500'} />
                        <Text fontSize="sm" color="whiteAlpha.700">
                          JWT: {maskedToken}
                        </Text>
                      </HStack>
                      {token && (
                        <Button size="xs" variant="ghost" onClick={handleLogout}>
                          Сбросить
                        </Button>
                      )}
                    </Flex>
                  </Tooltip>
                </Stack>
              </CardBody>
            </Card>
          </GridItem>

          <GridItem colSpan={{ base: 1, lg: 2 }}>
            <Card bg="whiteAlpha.100" border="1px solid" borderColor="whiteAlpha.200" backdropFilter="blur(12px)">
              <CardHeader>
                <Flex align="center" justify="space-between">
                  <Box>
                    <Heading size="md">Каталог товаров</Heading>
                    <Text fontSize="sm" color="whiteAlpha.700" mt={2}>
                      данные приходят из CatalogService, кэшируются в Redis
                    </Text>
                  </Box>
                  <HStack spacing={4} color="whiteAlpha.700">
                    <HStack>
                      <Icon as={FiBox} />
                      <Text>{products.length}</Text>
                    </HStack>
                    <Box w="1px" h="20px" bg="whiteAlpha.300" />
                    <Text fontSize="sm">RabbitMQ · Postgres · Redis · Cart {cart.reduce((a,c)=>a+c.quantity,0)}</Text>
                  </HStack>
                </Flex>
              </CardHeader>
              <CardBody>
                {catalogLoading ? (
                  <Center py={12}>
                    <Spinner size="lg" color="teal.300" />
                  </Center>
                ) : (
                  <Grid templateColumns={{ base: 'repeat(1, 1fr)', md: 'repeat(2, 1fr)', xl: 'repeat(3, 1fr)' }} gap={4}>
                    {products.map(product => (
                      <GridItem key={product.id}>
                        <Card bg="blackAlpha.500" border="1px solid" borderColor="whiteAlpha.200" h="100%">
                          <CardBody display="flex" flexDirection="column" justifyContent="space-between">
                            <Box>
                              <Heading size="sm" display="flex" alignItems="center" gap={2}>
                                <Icon as={FiShoppingBag} color="teal.300" />
                                {product.name}
                              </Heading>
                              <Text color="whiteAlpha.600" fontSize="sm" mt={2}>
                                #{product.id.slice(0, 8)}
                              </Text>
                            </Box>
                            <Box mt={6}>
                              <Text fontSize="lg" fontWeight="bold">{product.price} ₽</Text>
                              <Button
                                mt={3}
                                onClick={() => order(product)}
                                isLoading={loading}
                                loadingText="Создание"
                              >
                                Купить
                              </Button>
                              <Button mt={3} ml={3} variant="outline" onClick={() => addToCart(product)}>
                                В корзину
                              </Button>
                            </Box>
                          </CardBody>
                        </Card>
                      </GridItem>
                    ))}
                  </Grid>
                )}
              </CardBody>
            </Card>
            {!!cart.length && (
              <Card mt={4} bg="blackAlpha.500" border="1px solid" borderColor="whiteAlpha.200">
                <CardHeader>
                  <Heading size="sm">Корзина</Heading>
                </CardHeader>
                <CardBody>
                  <Stack spacing={2}>
                    {cart.map(ci => (
                      <Flex key={ci.product.id} align="center" justify="space-between">
                        <Text>{ci.product.name} × {ci.quantity}</Text>
                        <Text>{(ci.product.price * ci.quantity).toFixed(2)} ₽</Text>
                      </Flex>
                    ))}
                    <Flex align="center" justify="space_between" fontWeight="bold" pt={2}>
                      <Text>Итого</Text>
                      <Text>{cart.reduce((s, ci) => s + ci.product.price * ci.quantity, 0).toFixed(2)} ₽</Text>
                    </Flex>
                    <HStack pt={2}>
                      <Button onClick={checkoutCart} isLoading={loading} colorScheme="teal">Оформить</Button>
                      <Button onClick={clearCart} variant="ghost">Очистить</Button>
                    </HStack>
                  </Stack>
                </CardBody>
              </Card>
            )}
          </GridItem>
        </SimpleGrid>
        )}
      </Box>
      {banner && (
        <Center position="fixed" bottom={6} left="50%" transform="translateX(-50%)" px={4} py={3}
          bg={banner.status === 'success' ? 'green.500' : banner.status === 'error' ? 'red.500' : banner.status === 'warning' ? 'yellow.500' : 'blue.500'}
          color="white" borderRadius="md" boxShadow="lg">
          <Text fontSize="sm">{banner.text}</Text>
        </Center>
      )}
      <Box as="footer" py={6} borderTop="1px solid" borderColor="whiteAlpha.200">
        <Center color="whiteAlpha.500" fontSize="sm">
          Monolith + Microservices · Identity / Catalog / Order / Payment · RabbitMQ · Redis · Postgres · Docker
        </Center>
      </Box>
    </Flex>
  )
}



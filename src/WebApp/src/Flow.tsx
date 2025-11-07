import { useEffect, useMemo, useState } from 'react'
import { Box, Button, Card, CardBody, CardHeader, HStack, Heading, Input, SimpleGrid, Spinner, Text } from '@chakra-ui/react'
import { createOrderTraced, getProductsTraced, getTraceById, getTracesRecent, loginTraced, registerTraced } from './api'

type TraceEvent = { timestampUtc: string; service: string; message: string }

export function Flow() {
  const [traceId, setTraceId] = useState<string>('')
  const [events, setEvents] = useState<TraceEvent[]>([])
  const [recent, setRecent] = useState<TraceEvent[]>([])
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    // периодический опрос для обновления ленты и выбранного trace
    const t = setInterval(async () => {
      const e = await getTracesRecent()
      setRecent(e)
      if (traceId) {
        const ti = await getTraceById(traceId)
        setEvents(ti)
      }
    }, 2000)
    return () => clearInterval(t)
  }, [traceId])

  const startScenario = async () => {
    const id = crypto.randomUUID()
    setTraceId(id)
    setLoading(true)
    try {
      await registerTraced(id, 'user@test.com', 'P@ssw0rd!')
      await loginTraced(id, 'user@test.com', 'P@ssw0rd!')
      const products = await getProductsTraced(id)
      const p = products[0]
      await createOrderTraced(id, crypto.randomUUID(), { productId: p.id, quantity: 1, unitPrice: p.price })
      const ti = await getTraceById(id)
      setEvents(ti)
    } catch {
      // ignore here, timeline will still show
    } finally {
      setLoading(false)
    }
  }

  return (
    <SimpleGrid columns={{ base: 1, xl: 2 }} spacing={6}>
      <Card bg="whiteAlpha.100" border="1px solid" borderColor="whiteAlpha.200">
        <CardHeader>
          <Heading size="md">Сценарий</Heading>
        </CardHeader>
        <CardBody>
          <HStack mb={3}>
            <Input placeholder="Trace Id" value={traceId} onChange={e => setTraceId(e.target.value)} />
            <Button onClick={startScenario} isLoading={loading}>Запустить</Button>
          </HStack>
          {!events.length ? (
            <Text color="whiteAlpha.700">Запустите сценарий или введите Trace Id.</Text>
          ) : (
            <Box as="ul" fontFamily="mono" fontSize="sm" maxH="60vh" overflowY="auto">
              {events.map((e, i) => (
              <Box as="li" key={i} py={1} borderBottom="1px dashed" borderColor="whiteAlpha.200">
                  <Text>
                    {new Date(e.timestampUtc).toLocaleTimeString()} · [{e.service}] · {e.message}
                  </Text>
                </Box>
              ))}
            </Box>
          )}
        </CardBody>
      </Card>

      <Card bg="whiteAlpha.100" border="1px solid" borderColor="whiteAlpha.200">
        <CardHeader>
          <Heading size="md">Глобальная лента (последние)</Heading>
        </CardHeader>
        <CardBody>
          {!recent.length ? (
            <Spinner />
          ) : (
            <Box as="ul" fontFamily="mono" fontSize="sm" maxH="60vh" overflowY="auto">
              {recent.map((e, i) => (
                <Box as="li" key={i} py={1} borderBottom="1px dashed" borderColor="whiteAlpha.200">
                  <Text>
                    {new Date(e.timestampUtc).toLocaleTimeString()} · [{e.service}] · {e.message}
                  </Text>
                </Box>
              ))}
            </Box>
          )}
        </CardBody>
      </Card>
    </SimpleGrid>
  )
}



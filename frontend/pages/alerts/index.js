import { useState } from 'react';
import useSWR from 'swr';
import {
  ChakraProvider,
  Box,
  Heading,
  Flex,
  Input,
  Button,
  List,
  ListItem,
} from '@chakra-ui/react';

const fetcher = (url) => fetch(url).then((res) => res.json());

export default function AlertsPage() {
  const { data: subs, mutate } = useSWR('/api/alerts/subscriptions', fetcher);
  const [endpoint, setEndpoint] = useState('');

  const addSubscription = async () => {
    if (!endpoint) return;
    await fetch('/api/alerts/subscriptions', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ endpoint }),
    });
    setEndpoint('');
    mutate();
  };

  const removeSubscription = async (id) => {
    await fetch(`/api/alerts/subscriptions/${id}`, { method: 'DELETE' });
    mutate();
  };

  return (
    <ChakraProvider>
      <Box p={4}>
        <Heading mb={4}>Alert Subscriptions</Heading>
        <Flex mb={4}>
          <Input
            placeholder="Webhook URL"
            value={endpoint}
            onChange={(e) => setEndpoint(e.target.value)}
          />
          <Button ml={2} onClick={addSubscription}>
            Add
          </Button>
        </Flex>
        <List spacing={2}>
          {subs &&
            subs.map((s) => (
              <ListItem key={s.id}>
                <Flex justify="space-between">
                  <Box>{s.endpoint}</Box>
                  <Button
                    size="xs"
                    colorScheme="red"
                    onClick={() => removeSubscription(s.id)}
                  >
                    Remove
                  </Button>
                </Flex>
              </ListItem>
            ))}
        </List>
      </Box>
    </ChakraProvider>
  );
}

import { ChakraProvider, Box, Heading, Text, List, ListItem } from '@chakra-ui/react';
import useSWR from 'swr';

const fetcher = (url) => fetch(url).then((res) => res.json());

function HomePage() {
  const { data: iocs } = useSWR('/api/iocs', fetcher, { refreshInterval: 5000 });
  const { data: summary } = useSWR('/api/summary', fetcher, { refreshInterval: 10000 });

  return (
    <ChakraProvider>
      <Box p={4}>
        <Heading mb={4}>Live IOC Feed</Heading>
        <List spacing={2} mb={8}>
          {iocs && iocs.map((ioc) => (
            <ListItem key={ioc.id}>{ioc.source}: {ioc.ioc}</ListItem>
          ))}
        </List>
        <Heading size="md" mb={2}>AI Summary</Heading>
        <Text whiteSpace="pre-wrap">{summary ? summary.summary : 'Loading...'}</Text>
      </Box>
    </ChakraProvider>
  );
}

export default HomePage;

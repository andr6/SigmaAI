import { ChakraProvider, Box, Heading, List, ListItem } from '@chakra-ui/react';
import useSWR from 'swr';

const fetcher = (url) => fetch(url).then((res) => res.json());

export default function ReportPage() {
  const { data } = useSWR('/api/mitre', fetcher, { refreshInterval: 5000 });

  return (
    <ChakraProvider>
      <Box p={4}>
        <Heading mb={4}>MITRE Technique Mappings</Heading>
        <List spacing={2}>
          {data && data.map((id, idx) => (
            <ListItem key={idx}>{id}</ListItem>
          ))}
        </List>
      </Box>
    </ChakraProvider>
  );
}

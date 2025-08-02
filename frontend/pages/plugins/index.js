import { ChakraProvider, Box, Heading, Input, Button, List, ListItem, Text } from '@chakra-ui/react';
import { useState } from 'react';
import useSWR from 'swr';

const fetcher = (url) => fetch(url).then((res) => res.json());

export default function PluginPage() {
  const { data, mutate } = useSWR('/api/PluginStore/List', fetcher);
  const [file, setFile] = useState();
  const [description, setDescription] = useState('');

  const handleUpload = async () => {
    if (!file) return;
    const formData = new FormData();
    formData.append('file', file);
    if (description) formData.append('description', description);
    await fetch('/api/PluginStore/Upload', {
      method: 'POST',
      body: formData,
    });
    setFile(undefined);
    setDescription('');
    mutate();
  };

  return (
    <ChakraProvider>
      <Box p={4}>
        <Heading mb={4}>Plugin Store</Heading>
        <Box mb={4}>
          <Input type="file" onChange={(e) => setFile(e.target.files[0])} mb={2} />
          <Input placeholder="Description" value={description} onChange={(e) => setDescription(e.target.value)} mb={2} />
          <Button onClick={handleUpload} isDisabled={!file}>Upload Plugin</Button>
        </Box>
        <List spacing={2}>
          {data && data.map((p) => (
            <ListItem key={p.name}>
              <Text as="span" fontWeight="bold">{p.name}</Text> - {p.description}
            </ListItem>
          ))}
        </List>
      </Box>
    </ChakraProvider>
  );
}

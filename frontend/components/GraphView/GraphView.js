import dynamic from 'next/dynamic';
import { Box } from '@chakra-ui/react';

const ForceGraph2D = dynamic(() => import('react-force-graph').then(mod => mod.ForceGraph2D), {
  ssr: false,
});

export default function GraphView({ data }) {
  if (!data) return null;
  return (
    <Box width="100%" height="600px">
      <ForceGraph2D graphData={data} />
    </Box>
  );
}

import { render, screen } from '@testing-library/react';
import HomePage from '../pages/index';

jest.mock('swr', () => ({
  __esModule: true,
  default: (key) => {
    if (key === '/api/iocs') {
      return { data: [{ id: 1, source: 'Test', ioc: '1.1.1.1' }] };
    }
    if (key === '/api/summary') {
      return { data: { summary: 'Summary text' } };
    }
    return { data: null };
  },
}));

test('renders iocs and summary', () => {
  render(<HomePage />);
  expect(screen.getByText('Live IOC Feed')).toBeInTheDocument();
  expect(screen.getByText('AI Summary')).toBeInTheDocument();
  expect(screen.getByText('Test: 1.1.1.1')).toBeInTheDocument();
  expect(screen.getByText('Summary text')).toBeInTheDocument();
});

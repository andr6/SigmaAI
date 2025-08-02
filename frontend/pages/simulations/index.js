import { useEffect, useState } from 'react';

export default function Simulations() {
  const [results, setResults] = useState([]);

  const loadResults = () => {
    fetch('/api/simulations')
      .then(res => res.json())
      .then(setResults)
      .catch(() => setResults([]));
  };

  const runSimulation = async () => {
    await fetch('/api/simulations/schedule', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ testId: 'T1003' })
    });
    setTimeout(loadResults, 1000);
  };

  useEffect(() => {
    loadResults();
  }, []);

  return (
    <div>
      <h1>Simulation Results</h1>
      <button onClick={runSimulation}>Run Simulation</button>
      <ul>
        {results.map((r, idx) => (
          <li key={idx}>{r}</li>
        ))}
      </ul>
    </div>
  );
}

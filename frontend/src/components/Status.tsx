import type { ReactNode } from 'react';

interface Props {
  loading: boolean;
  error: string | null;
  children: ReactNode;
}

export default function Status({ loading, error, children }: Props) {
  if (loading) return <p>Loading…</p>;
  if (error) return <p className="error">{error}</p>;
  return <>{children}</>;
}

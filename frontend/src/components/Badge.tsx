import { ReactNode } from 'react';

export function Badge({ tone, children }: { tone: 'ok' | 'low' | 'neutral'; children: ReactNode }) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}

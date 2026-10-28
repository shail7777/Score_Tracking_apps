import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from './client';

export function useFetch<T>(fetcher: () => Promise<T>, deps: unknown[] = []) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [errorStatus, setErrorStatus] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const latest = useRef(0);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const load = useCallback(fetcher, deps);

  const reload = useCallback(() => {
    const call = ++latest.current;
    setLoading(true);
    setError(null);
    setErrorStatus(null);
    load()
      .then((d) => call === latest.current && setData(d))
      .catch((e: Error) => {
        if (call !== latest.current) return;
        setError(e.message);
        setErrorStatus(e instanceof ApiError ? e.status : null);
      })
      .finally(() => call === latest.current && setLoading(false));
  }, [load]);

  useEffect(reload, [reload]);

  return { data, error, errorStatus, loading, reload };
}

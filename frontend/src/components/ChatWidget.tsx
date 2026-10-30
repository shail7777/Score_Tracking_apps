import { FormEvent, useEffect, useRef, useState } from 'react';
import { api } from '../api/client';
import type { ChatMessage } from '../types/nba';

const GREETING: ChatMessage = {
  role: 'assistant',
  content: 'Hi! Ask me anything about using the NBA App.',
};

export default function ChatWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([GREETING]);
  const [input, setInput] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView?.({ block: 'end' });
  }, [messages, busy, open]);

  const send = async (e: FormEvent) => {
    e.preventDefault();
    const text = input.trim();
    if (!text || busy) return;

    const next: ChatMessage[] = [...messages, { role: 'user', content: text }];
    setMessages(next);
    setInput('');
    setError(null);
    setBusy(true);
    try {
      // The greeting is UI-only; the model gets the real conversation.
      const { reply } = await api.chat.ask(next.filter((m) => m !== GREETING));
      setMessages([...next, { role: 'assistant', content: reply }]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong.');
    } finally {
      setBusy(false);
    }
  };

  if (!open) {
    return (
      <button type="button" className="chat-launcher" onClick={() => setOpen(true)} aria-label="Open help chat">
        💬
      </button>
    );
  }

  return (
    <section className="chat-panel" aria-label="Help chat">
      <header className="chat-header">
        <span>Help assistant</span>
        <button type="button" onClick={() => setOpen(false)} aria-label="Close help chat">✕</button>
      </header>
      <div className="chat-messages" role="log" aria-live="polite">
        {messages.map((m, i) => (
          <div key={i} className={`chat-msg chat-msg-${m.role}`}>{m.content}</div>
        ))}
        {busy && <div className="chat-msg chat-msg-assistant chat-typing">Thinking…</div>}
        {error && <div className="chat-error" role="alert">{error}</div>}
        <div ref={bottomRef} />
      </div>
      <form className="chat-form" onSubmit={send}>
        <input
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Ask a question…"
          aria-label="Your question"
          maxLength={1000}
        />
        <button type="submit" disabled={busy || !input.trim()}>Send</button>
      </form>
    </section>
  );
}

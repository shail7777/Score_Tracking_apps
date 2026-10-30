import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, api } from '../api/client';
import ChatWidget from '../components/ChatWidget';

afterEach(cleanup);

const openChat = () => {
  render(<ChatWidget />);
  fireEvent.click(screen.getByRole('button', { name: 'Open help chat' }));
};

const ask = (text: string) => {
  fireEvent.change(screen.getByRole('textbox', { name: 'Your question' }), { target: { value: text } });
  fireEvent.click(screen.getByRole('button', { name: 'Send' }));
};

describe('ChatWidget', () => {
  it('is collapsed until the launcher is clicked', () => {
    render(<ChatWidget />);
    expect(screen.queryByRole('textbox', { name: 'Your question' })).toBeNull();

    fireEvent.click(screen.getByRole('button', { name: 'Open help chat' }));

    expect(screen.getByRole('textbox', { name: 'Your question' })).toBeTruthy();
  });

  it('sends the conversation (without the greeting) and shows the reply', async () => {
    const spy = vi.spyOn(api.chat, 'ask').mockResolvedValue({ reply: 'Press +2 or +3.' });
    openChat();

    ask('How do I score?');

    expect(await screen.findByText('Press +2 or +3.')).toBeTruthy();
    expect(screen.getByText('How do I score?')).toBeTruthy();
    expect(spy).toHaveBeenCalledWith([{ role: 'user', content: 'How do I score?' }]);
  });

  it('shows an error when the assistant is unavailable and lets the user retry', async () => {
    vi.spyOn(api.chat, 'ask')
      .mockRejectedValueOnce(new ApiError('Could not reach Ollama. Is it running?', 503))
      .mockResolvedValueOnce({ reply: 'Back online.' });
    openChat();

    ask('hello');
    expect((await screen.findByRole('alert')).textContent).toContain('Could not reach Ollama');

    ask('hello again');
    expect(await screen.findByText('Back online.')).toBeTruthy();
    await waitFor(() => expect(screen.queryByRole('alert')).toBeNull());
  });

  it('ignores blank questions', () => {
    const spy = vi.spyOn(api.chat, 'ask');
    openChat();

    ask('   ');

    expect(spy).not.toHaveBeenCalled();
  });
});

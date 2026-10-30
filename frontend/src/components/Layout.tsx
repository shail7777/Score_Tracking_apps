import { NavLink, Outlet } from 'react-router-dom';
import ChatWidget from './ChatWidget';

export default function Layout() {
  return (
    <>
      <header className="header">
        <h1>🏀 NBA App</h1>
        <nav>
          <NavLink to="/" end>Home</NavLink>
          <NavLink to="/players">Players</NavLink>
          <NavLink to="/games" end>Games</NavLink>
        </nav>
      </header>
      <main className="container">
        <Outlet />
      </main>
      <ChatWidget />
    </>
  );
}

import { Navigate, Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import GamesPage from './pages/GamesPage';
import HomePage from './pages/HomePage';
import PlayersPage from './pages/PlayersPage';
import ScorecardPage from './pages/ScorecardPage';
import TeamDetailPage from './pages/TeamDetailPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="teams" element={<Navigate to="/" replace />} />
        <Route path="teams/:id" element={<TeamDetailPage />} />
        <Route path="players" element={<PlayersPage />} />
        <Route path="games" element={<GamesPage />} />
        <Route path="games/:gameId/scorecard" element={<ScorecardPage />} />
      </Route>
    </Routes>
  );
}

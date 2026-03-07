import { Routes, Route } from 'react-router-dom';
import Layout from './components/Layout';
import HomePage from './pages/HomePage';
import WatDoenWePage from './pages/WatDoenWePage';
import WieZijnWePage from './pages/WieZijnWePage';
import ActiviteitenPage from './pages/ActiviteitenPage';
import BalParentalPage from './pages/BalParentalPage';
import ContactPage from './pages/ContactPage';
import NotFoundPage from './pages/NotFoundPage';

function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<HomePage />} />
        <Route path="wat-doen-we" element={<WatDoenWePage />} />
        <Route path="wie-zijn-we" element={<WieZijnWePage />} />
        <Route path="activiteiten" element={<ActiviteitenPage />} />
        <Route path="bal-parental" element={<BalParentalPage />} />
        <Route path="contact" element={<ContactPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}

export default App;

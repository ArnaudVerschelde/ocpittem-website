import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import HomePage from './pages/HomePage';
import WatDoenWePage from './pages/WatDoenWePage';
import WieZijnWePage from './pages/WieZijnWePage';
import ActiviteitenPage from './pages/ActiviteitenPage';
import BalParentalPage from './pages/BalParentalPage';
import SfeerbeeldenBalParental2026Page from './pages/SfeerbeeldenBalParental2026Page';
import ContactPage from './pages/ContactPage';
import NotFoundPage from './pages/NotFoundPage';
import PaymentSuccessPage from './pages/PaymentSuccessPage';
import PaymentCancelPage from './pages/PaymentCancelPage';
import ScanPage from './pages/ScanPage';
import PrivacyPage from './pages/PrivacyPage';
import { BAL_PARENTAL_ACTIVE } from './config/balParental';

function App() {
    return (
        <Routes>
            {/* Oude paden opvangen */}
            <Route path="/nl" element={<Navigate to="/" replace />} />
            <Route path="/nl/*" element={<Navigate to="/" replace />} />

            {/* Standalone pagina's zonder Navbar/Footer */}
            <Route path="scan" element={<ScanPage />} />

            <Route path="/" element={<Layout />}>
                <Route index element={<HomePage />} />
                <Route path="wat-doen-we" element={<WatDoenWePage />} />
                <Route path="wie-zijn-we" element={<WieZijnWePage />} />
                <Route path="activiteiten" element={<ActiviteitenPage />} />
                <Route
                    path="sfeerbeelden-bal-parental-2026"
                    element={<SfeerbeeldenBalParental2026Page />}
                />
                {BAL_PARENTAL_ACTIVE && (
                    <Route path="bal-parental" element={<BalParentalPage />} />
                )}
                <Route path="contact" element={<ContactPage />} />

                {/* Stripe return pages */}
                <Route path="betaling/success" element={<PaymentSuccessPage />} />
                <Route path="betaling/cancel" element={<PaymentCancelPage />} />

                <Route path="*" element={<NotFoundPage />} />

                <Route path="privacy" element={<PrivacyPage />} />
            </Route>
        </Routes>
    );
}

export default App;
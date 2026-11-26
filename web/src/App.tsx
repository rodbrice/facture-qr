import { useEffect, useRef } from 'react'
import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router'
import { usePageTitle } from './components/usePageTitle'
import { CustomerPage } from './pages/CustomerPage'
import { CustomersPage } from './pages/CustomersPage'
import { InvoicePage } from './pages/InvoicePage'
import { InvoicesPage } from './pages/InvoicesPage'
import { SettingsPage } from './pages/SettingsPage'

// The page paths are in French and never overlap the API paths (/customers, /invoices, /settings);
// the API serves index.html for them (FrontEnd.Pages in src/FactureQr.Api).
export function App() {
  const mainRef = useRef<HTMLElement>(null)
  const { pathname } = useLocation()
  const shownPath = useRef(pathname)

  // After a change of page, the focus goes to the main content, as it would after a full page load.
  useEffect(() => {
    if (shownPath.current !== pathname) {
      shownPath.current = pathname
      mainRef.current?.focus()
    }
  }, [pathname])

  return (
    <>
      <a className="skip-link" href="#contenu">
        Aller au contenu
      </a>
      <header className="app-header">
        <p className="app-name">Facture QR</p>
        <nav aria-label="Navigation principale">
          <ul>
            <li>
              <NavLink to="/factures">Factures</NavLink>
            </li>
            <li>
              <NavLink to="/clients">Clients</NavLink>
            </li>
            <li>
              <NavLink to="/parametres">Paramètres</NavLink>
            </li>
          </ul>
        </nav>
      </header>
      <main id="contenu" ref={mainRef} tabIndex={-1}>
        <Routes>
          <Route path="/" element={<Navigate to="/factures" replace />} />
          <Route path="/factures" element={<InvoicesPage />} />
          <Route path="/factures/:id" element={<InvoicePage />} />
          <Route path="/clients" element={<CustomersPage />} />
          <Route path="/clients/nouveau" element={<CustomerPage />} />
          <Route path="/clients/:id" element={<CustomerPage />} />
          <Route path="/parametres" element={<SettingsPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </main>
    </>
  )
}

function NotFound() {
  usePageTitle('Page introuvable')
  return (
    <>
      <h1>Page introuvable</h1>
      <p>
        Cette adresse ne correspond à aucune page. <NavLink to="/factures">Retour aux factures</NavLink>
      </p>
    </>
  )
}

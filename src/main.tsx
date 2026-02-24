import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '@/shared/lib/i18n/i18n'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

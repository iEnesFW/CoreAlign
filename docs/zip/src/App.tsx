import { Sidebar } from './components/Sidebar';
import { Header } from './components/Header';
import { WizardContainer } from './components/WizardContainer';

export default function App() {
  return (
    <div className="flex h-screen overflow-hidden bg-[#141824] text-slate-200 font-sans">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0 bg-[#141824]">
        <Header />
        <WizardContainer />
      </div>
    </div>
  );
}

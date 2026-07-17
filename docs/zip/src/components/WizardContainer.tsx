import { useState } from 'react';
import { Step1 } from './Step1';
import { Step2 } from './Step2';
import { Check, ChevronRight } from 'lucide-react';
import { motion, AnimatePresence } from 'motion/react';

export function WizardContainer() {
  const [step, setStep] = useState(1);

  const steps = [
    { id: 1, title: 'Customer Details', description: 'Basic info & conditions' },
    { id: 2, title: 'Order Items & Summary', description: 'Products & financial totals' }
  ];

  return (
    <div className="flex-1 flex flex-col h-full overflow-hidden bg-[#141824] relative">
      {/* Scrollable Content Area */}
      <div className="flex-1 overflow-y-auto px-4 sm:px-8 py-6">
        
        {/* Header & Title */}
        <div className="mb-8 max-w-7xl mx-auto">
          <div className="flex items-center gap-4 mb-8">
            <div className="w-12 h-12 rounded-xl bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center shadow-sm">
              <div className="w-6 h-6 rounded flex items-center justify-center">
                <svg className="w-6 h-6 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
              </div>
            </div>
            <div>
              <h1 className="text-2xl font-semibold text-white tracking-tight">Create New Order</h1>
              <p className="text-sm text-slate-400 mt-1">Fill out the details below to generate a new customer order.</p>
            </div>
          </div>

          {/* Stepper */}
          <div className="flex items-center max-w-2xl bg-[#1b202e] border border-[#2a3143] rounded-xl p-2 shadow-sm">
            {steps.map((s, idx) => {
              const isActive = step === s.id;
              const isPast = step > s.id;
              return (
                <div key={s.id} className="flex items-center flex-1 last:flex-none">
                  <div 
                    className="flex items-center gap-3 group px-4 py-2 rounded-lg cursor-pointer transition-colors hover:bg-[#2a3143]/50 w-full" 
                    onClick={() => setStep(s.id)} 
                    role="button"
                  >
                    <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium transition-colors flex-shrink-0 ${
                      isActive ? 'bg-indigo-600 text-white shadow-[0_0_15px_rgba(79,70,229,0.4)]' :
                      isPast ? 'bg-emerald-500 text-white shadow-[0_0_15px_rgba(16,185,129,0.3)]' :
                      'bg-[#0f111a] text-slate-400 border border-[#2a3143]'
                    }`}>
                      {isPast ? <Check size={16} strokeWidth={3} /> : s.id}
                    </div>
                    <div className="flex flex-col min-w-0">
                      <span className={`text-sm font-semibold truncate transition-colors ${isActive ? 'text-indigo-400' : isPast ? 'text-slate-200' : 'text-slate-500'}`}>
                        {s.title}
                      </span>
                      <span className="text-[10px] text-slate-500 hidden sm:block truncate">{s.description}</span>
                    </div>
                  </div>
                  {idx < steps.length - 1 && (
                    <div className="w-8 h-px bg-[#2a3143] shrink-0 mx-2 relative hidden sm:block">
                      <div className={`absolute left-0 top-0 h-full bg-emerald-500 transition-all duration-300`} style={{ width: isPast ? '100%' : '0%' }}></div>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        {/* Step Content */}
        <div className="max-w-7xl mx-auto">
          <AnimatePresence mode="wait">
            <motion.div
              key={step}
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: -20 }}
              transition={{ duration: 0.2 }}
            >
              {step === 1 ? <Step1 /> : <Step2 />}
            </motion.div>
          </AnimatePresence>
        </div>
      </div>

      {/* Fixed Bottom Action Bar */}
      <div className="absolute bottom-0 left-0 right-0 bg-[#141824]/90 backdrop-blur-md border-t border-[#2a3143] p-4 px-4 sm:px-8 flex items-center justify-between z-20">
        <div className="flex items-center gap-3">
          <button className="px-4 py-2.5 text-sm font-medium text-slate-400 hover:text-white transition-colors">
            Cancel
          </button>
          <button className="hidden sm:block px-4 py-2.5 text-sm font-medium text-slate-300 bg-[#1b202e] hover:bg-[#2a3143] border border-[#2a3143] rounded-md transition-colors shadow-sm">
            Save as Draft
          </button>
        </div>
        
        <div className="flex items-center gap-3">
          {step > 1 && (
            <button 
              onClick={() => setStep(step - 1)}
              className="px-4 py-2.5 text-sm font-medium text-slate-300 bg-[#1b202e] hover:bg-[#2a3143] border border-[#2a3143] rounded-md transition-colors shadow-sm"
            >
              Back
            </button>
          )}
          {step < 2 ? (
            <button 
              onClick={() => setStep(step + 1)}
              className="px-6 py-2.5 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-500 rounded-md transition-colors shadow-sm shadow-indigo-500/20 flex items-center gap-2"
            >
              Continue <ChevronRight size={16} />
            </button>
          ) : (
            <button className="px-6 py-2.5 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-500 rounded-md transition-colors shadow-sm shadow-indigo-500/20 flex items-center gap-2">
              <Check size={16} />
              Create Order
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

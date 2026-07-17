import React, { useState, useEffect } from 'react';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { useQuery } from '@tanstack/react-query';
import type { KioskStepDto } from '@/features/manufacturing/api/manufacturingApi';
import { kioskApi } from '@/features/manufacturing/api/manufacturingApi';
import {
  useStartJobStep,
  useFinishJobStep,
} from '@/features/manufacturing/hooks/useManufacturingQueries';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { Play, Square, KeyRound, UserCog, Package, Clock, AlertTriangle } from 'lucide-react';
import { toast } from 'sonner';

export const ManufacturingKioskPage: React.FC = () => {
  const [pinCode, setPinCode] = useState('');
  const [operatorId, setOperatorId] = useState('');
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [workCenterId, setWorkCenterId] = useState('');
  const [employeeId, setEmployeeId] = useState('');

  // Real-time clock for kiosk
  const [time, setTime] = useState(new Date());
  useEffect(() => {
    const timer = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  const { data: activeSteps } = useQuery({
    queryKey: ['kiosk-active-steps', workCenterId],
    queryFn: () => kioskApi.getActiveSteps(workCenterId),
    enabled: isAuthenticated && !!workCenterId,
    refetchInterval: 15000,
  });

  const { mutateAsync: startStep } = useStartJobStep();
  const { mutateAsync: finishStep } = useFinishJobStep();

  const handleLogin = async (e?: React.FormEvent) => {
    e?.preventDefault();
    if (!operatorId || !pinCode) {
      toast.error('Please enter Operator ID and PIN');
      return;
    }
    const [response] = await safeRequestWithNotify(kioskApi.verifyPin(operatorId, pinCode));
    if (response?.isSuccess && response.data?.workCenterId) {
      setIsAuthenticated(true);
      setWorkCenterId(response.data.workCenterId);
      setEmployeeId(response.data.employeeId);
    }
  };

  const handleStart = async (step: KioskStepDto) => {
    await safeRequestWithNotify(
      startStep({
        id: step.jobId,
        stepNumber: step.stepNumber,
        input: { operatorId: employeeId },
      }),
      { successMessage: 'Step Started!' },
    );
  };

  const handleFinish = async (step: KioskStepDto) => {
    const goodStr = window.prompt(
      `Enter Good Quantity (Max ${step.inputQuantity}):`,
      String(step.inputQuantity),
    );
    if (goodStr === null) return;
    const goodQty = Number(goodStr) || 0;

    const scrapStr = window.prompt(`Enter Scrapped Quantity:`, '0');
    if (scrapStr === null) return;
    const scrapQty = Number(scrapStr) || 0;

    await safeRequestWithNotify(
      finishStep({
        id: step.jobId,
        stepNumber: step.stepNumber,
        input: { operatorId: employeeId, goodQuantity: goodQty, scrappedQuantity: scrapQty },
      }),
      { successMessage: 'Step Finished!' },
    );
  };

  if (!isAuthenticated) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-900 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-indigo-900 via-slate-900 to-black">
        <div className="absolute top-8 right-8 text-white/50 font-mono text-xl tracking-widest">
          {time.toLocaleTimeString()}
        </div>

        <div className="w-full max-w-md relative">
          <div className="absolute inset-0 bg-indigo-500 rounded-3xl blur-3xl opacity-20 animate-pulse"></div>
          <form
            onSubmit={handleLogin}
            className="relative space-y-8 rounded-3xl bg-white/10 p-10 shadow-2xl backdrop-blur-xl border border-white/10"
          >
            <div className="text-center">
              <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 shadow-lg shadow-indigo-500/30 mb-6">
                <UserCog className="h-8 w-8 text-white" />
              </div>
              <h2 className="text-3xl font-bold text-white tracking-tight">Kiosk Login</h2>
              <p className="mt-2 text-indigo-200/60">Scan barcode or enter your ID</p>
            </div>

            <div className="space-y-5">
              <div className="relative">
                <Input
                  id="operator-id"
                  name="operatorId"
                  type="text"
                  required
                  placeholder="Operator ID / Badge"
                  value={operatorId}
                  onChange={(e) => setOperatorId(e.target.value)}
                  className="bg-white/5 border-white/10 text-white placeholder:text-white/30 rounded-xl h-14 pl-12 text-lg focus:ring-indigo-500 focus:border-indigo-500"
                  icon={<UserCog className="h-5 w-5 text-white/50" />}
                />
              </div>
              <div className="relative">
                <Input
                  id="pin-code"
                  name="pinCode"
                  type="password"
                  required
                  placeholder="PIN Code"
                  value={pinCode}
                  onChange={(e) => setPinCode(e.target.value)}
                  className="bg-white/5 border-white/10 text-white placeholder:text-white/30 rounded-xl h-14 pl-12 text-lg focus:ring-indigo-500 focus:border-indigo-500 font-mono tracking-widest"
                  icon={<KeyRound className="h-5 w-5 text-white/50" />}
                />
              </div>
            </div>

            <Button
              type="submit"
              className="w-full h-14 text-lg font-semibold rounded-xl bg-gradient-to-r from-indigo-600 to-purple-600 hover:from-indigo-500 hover:to-purple-500 border-0 shadow-[0_0_20px_rgba(79,70,229,0.3)] transition-all hover:shadow-[0_0_30px_rgba(79,70,229,0.5)]"
            >
              Access Terminal
            </Button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-900 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-indigo-900/40 via-slate-900 to-black p-8 text-slate-100">
      <header className="flex justify-between items-center mb-10 bg-white/5 backdrop-blur-md border border-white/10 rounded-2xl p-6 shadow-xl">
        <div>
          <h1 className="text-3xl font-bold text-white bg-clip-text text-transparent bg-gradient-to-r from-indigo-400 to-purple-400">
            Manufacturing Kiosk
          </h1>
          <p className="text-indigo-200/60 mt-1 flex items-center gap-2">
            <UserCog size={16} /> Logged in: {employeeId}
          </p>
        </div>
        <div className="text-right">
          <div className="text-4xl font-mono tracking-wider font-light text-white drop-shadow-[0_0_10px_rgba(255,255,255,0.3)]">
            {time.toLocaleTimeString()}
          </div>
          <div className="text-indigo-200/60 mt-1">
            {time.toLocaleDateString(undefined, {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric',
            })}
          </div>
        </div>
      </header>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        {activeSteps?.data?.map((step) => (
          <div
            key={`${step.jobId}-${step.stepNumber}`}
            className="group relative bg-white/10 backdrop-blur-xl border border-white/10 rounded-3xl p-6 shadow-2xl transition-all hover:bg-white-[0.15] hover:border-indigo-500/50"
          >
            <div className="absolute inset-0 bg-gradient-to-br from-indigo-500/10 to-purple-500/10 opacity-0 group-hover:opacity-100 transition-opacity rounded-3xl"></div>

            <div className="relative z-10">
              <div className="flex justify-between items-start mb-6">
                <div>
                  <h3 className="text-2xl font-bold text-white">{step.jobNumber as string}</h3>
                  <p className="text-indigo-300 font-medium text-sm mt-1 flex items-center gap-2">
                    <Package size={14} /> {step.productName}
                  </p>
                </div>
                <span
                  className={`px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-wider ${
                    step.status === 'InProgress'
                      ? 'bg-blue-500/20 text-blue-300 border border-blue-500/30 shadow-[0_0_10px_rgba(59,130,246,0.3)]'
                      : 'bg-yellow-500/20 text-yellow-300 border border-yellow-500/30 shadow-[0_0_10px_rgba(234,179,8,0.2)]'
                  }`}
                >
                  {step.status}
                </span>
              </div>

              <div className="space-y-4 mb-8">
                <div className="flex justify-between items-center p-3 rounded-xl bg-black/20 border border-white/5">
                  <span className="text-slate-400 text-sm">Operation</span>
                  <span className="text-white font-medium">{step.operationName}</span>
                </div>
                <div className="flex justify-between items-center p-3 rounded-xl bg-black/20 border border-white/5">
                  <span className="text-slate-400 text-sm">Quantity</span>
                  <span className="text-white font-medium text-lg">{step.inputQuantity}</span>
                </div>
                <div className="flex justify-between items-center p-3 rounded-xl bg-black/20 border border-white/5">
                  <span className="text-slate-400 text-sm flex items-center gap-2">
                    <Clock size={14} /> Setup / Run
                  </span>
                  <span className="text-white font-medium">
                    {step.setupTimeMinutes}m / {step.runTimeMinutesPerUnit}m
                  </span>
                </div>
              </div>

              <div className="flex gap-4">
                {step.status !== 'InProgress' && (
                  <Button
                    onClick={() => handleStart(step)}
                    className="flex-1 h-12 rounded-xl bg-emerald-500/20 text-emerald-300 border border-emerald-500/50 hover:bg-emerald-500 hover:text-white transition-all shadow-[0_0_15px_rgba(16,185,129,0.2)]"
                  >
                    <Play className="mr-2 h-5 w-5" /> Start
                  </Button>
                )}
                {step.status === 'InProgress' && (
                  <Button
                    onClick={() => handleFinish(step)}
                    className="flex-1 h-12 rounded-xl bg-indigo-500 text-white border-0 hover:bg-indigo-400 transition-all shadow-[0_0_20px_rgba(79,70,229,0.4)]"
                  >
                    <Square className="mr-2 h-5 w-5" /> Finish Step
                  </Button>
                )}
              </div>
            </div>
          </div>
        ))}
        {(!activeSteps?.data || activeSteps.data.length === 0) && (
          <div className="col-span-full py-20 text-center">
            <div className="inline-flex h-20 w-20 items-center justify-center rounded-full bg-white/5 border border-white/10 mb-6 shadow-xl">
              <AlertTriangle className="h-10 w-10 text-indigo-300/50" />
            </div>
            <h3 className="text-xl font-medium text-indigo-100">No active jobs</h3>
            <p className="text-slate-400 mt-2">
              There are currently no jobs queued for this work center.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

import React, { useState } from 'react';
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

export const ManufacturingKioskPage: React.FC = () => {
  const [pinCode, setPinCode] = useState('');
  const [operatorId, setOperatorId] = useState('');
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [workCenterId, setWorkCenterId] = useState('');
  const [employeeId, setEmployeeId] = useState('');

  const { data: activeSteps } = useQuery({
    queryKey: ['kiosk-active-steps', workCenterId],
    queryFn: () => kioskApi.getActiveSteps(workCenterId),
    enabled: isAuthenticated && !!workCenterId,
  });

  const { mutateAsync: startStep } = useStartJobStep();
  const { mutateAsync: finishStep } = useFinishJobStep();

  const handleLogin = async () => {
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
      <div className="flex h-screen items-center justify-center bg-gray-50 dark:bg-gray-900">
        <div className="w-full max-w-md space-y-8 rounded-lg bg-white p-10 shadow-xl dark:bg-gray-800">
          <div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
              Work Center Kiosk
            </h2>
          </div>
          <div className="mt-8 space-y-6">
            <div className="rounded-md shadow-sm -space-y-px">
              <div className="mb-4">
                <label htmlFor="operator-id" className="sr-only">
                  Operator ID
                </label>
                <Input
                  id="operator-id"
                  name="operatorId"
                  type="text"
                  required
                  placeholder="Operator ID (Guid)"
                  value={operatorId}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                    setOperatorId(e.target.value)
                  }
                />
              </div>
              <div>
                <label htmlFor="pin-code" className="sr-only">
                  PIN Code
                </label>
                <Input
                  id="pin-code"
                  name="pinCode"
                  type="password"
                  required
                  placeholder="PIN Code"
                  value={pinCode}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) => setPinCode(e.target.value)}
                />
              </div>
            </div>

            <div>
              <Button onClick={handleLogin} className="w-full">
                Sign In
              </Button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-6">Active Jobs at Work Center</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {activeSteps?.data?.map((step) => (
          <div
            key={`${step.jobId}-${step.stepNumber}`}
            className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6"
          >
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-xl font-semibold">{step.jobNumber as string}</h3>
              <span
                className={`px-3 py-1 rounded-full text-sm font-medium ${
                  step.status === 'InProgress'
                    ? 'bg-blue-100 text-blue-800'
                    : 'bg-yellow-100 text-yellow-800'
                }`}
              >
                {step.status}
              </span>
            </div>
            <p className="text-gray-600 dark:text-gray-300 mb-2">
              <strong>Product:</strong> {step.productName}
            </p>
            <p className="text-gray-600 dark:text-gray-300 mb-2">
              <strong>Operation:</strong> {step.operationName}
            </p>
            <p className="text-gray-600 dark:text-gray-300 mb-4">
              <strong>Input Qty:</strong> {step.inputQuantity}
            </p>

            <div className="flex space-x-3">
              {step.status === 'Pending' && (
                <Button
                  className="w-full bg-blue-600 hover:bg-blue-700"
                  onClick={() => handleStart(step)}
                >
                  Start
                </Button>
              )}
              {step.status === 'InProgress' && (
                <Button
                  className="w-full bg-green-600 hover:bg-green-700"
                  onClick={() => handleFinish(step)}
                >
                  Finish
                </Button>
              )}
            </div>
          </div>
        ))}
        {(!activeSteps?.data || activeSteps.data.length === 0) && (
          <div className="col-span-full text-center text-gray-500 py-10">
            No active jobs for this work center.
          </div>
        )}
      </div>
    </div>
  );
};

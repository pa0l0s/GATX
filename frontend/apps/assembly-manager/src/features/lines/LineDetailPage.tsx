import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { getWorkstations } from '../../shared/api/workstationsApi';
import {
  Allocation,
  allocateWorkstation,
  getAllocations,
  getAssemblyLine,
  removeAllocation,
  reorderAllocations
} from '../../shared/api/linesApi';

export function LineDetailPage() {
  const { id = '' } = useParams();
  const queryClient = useQueryClient();
  const [toAdd, setToAdd] = useState('');

  const lineQuery = useQuery({ queryKey: ['line', id], queryFn: () => getAssemblyLine(id) });
  const allocationsQuery = useQuery({ queryKey: ['allocations', id], queryFn: () => getAllocations(id) });
  const workstationsQuery = useQuery({ queryKey: ['workstations'], queryFn: getWorkstations });

  const setAllocations = (data: Allocation[]) => {
    queryClient.setQueryData(['allocations', id], data);
    queryClient.invalidateQueries({ queryKey: ['lines'] });
    queryClient.invalidateQueries({ queryKey: ['line', id] });
  };

  const allocateMutation = useMutation({
    mutationFn: (workstationId: string) => allocateWorkstation(id, workstationId),
    onSuccess: (data) => {
      setToAdd('');
      setAllocations(data);
    }
  });

  const removeMutation = useMutation({
    mutationFn: (workstationId: string) => removeAllocation(id, workstationId),
    onSuccess: setAllocations
  });

  const reorderMutation = useMutation({
    mutationFn: (workstationIds: string[]) => reorderAllocations(id, workstationIds),
    onSuccess: setAllocations
  });

  const allocations = allocationsQuery.data ?? [];
  const allocatedIds = new Set(allocations.map((allocation) => allocation.workstationId));
  const available = (workstationsQuery.data ?? []).filter((workstation) => !allocatedIds.has(workstation.id));

  function move(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= allocations.length) {
      return;
    }
    const ids = allocations.map((allocation) => allocation.workstationId);
    [ids[index], ids[target]] = [ids[target], ids[index]];
    reorderMutation.mutate(ids);
  }

  const actionError = allocateMutation.error ?? removeMutation.error ?? reorderMutation.error;
  const busy = allocateMutation.isPending || removeMutation.isPending || reorderMutation.isPending;

  return (
    <section className="panel">
      <div className="panel-head">
        <div>
          <Link className="back-link" to="/lines">
            ← Assembly lines
          </Link>
          <h1>{lineQuery.data ? lineQuery.data.name : 'Assembly line'}</h1>
          {lineQuery.data && (
            <p className="subtitle">
              {lineQuery.data.productName} ·{' '}
              <span className={lineQuery.data.active ? 'pill pill-on' : 'pill pill-off'}>
                {lineQuery.data.active ? 'Active' : 'Inactive'}
              </span>
            </p>
          )}
        </div>
      </div>

      {lineQuery.isError && <p className="error">{(lineQuery.error as Error).message}</p>}

      <div className="form">
        <select value={toAdd} onChange={(event) => setToAdd(event.target.value)} aria-label="Workstation to allocate">
          <option value="">Add a workstation…</option>
          {available.map((workstation) => (
            <option key={workstation.id} value={workstation.id}>
              {workstation.shortName} — {workstation.name}
            </option>
          ))}
        </select>
        <button
          type="button"
          disabled={!toAdd || allocateMutation.isPending}
          onClick={() => allocateMutation.mutate(toAdd)}
        >
          Allocate
        </button>
      </div>

      {actionError && <p className="error">{(actionError as Error).message}</p>}
      {allocationsQuery.isLoading && <p>Loading allocations…</p>}

      <ol className="allocations">
        {allocations.map((allocation, index) => (
          <li key={allocation.workstationId} className="allocation">
            <span className="position">{allocation.position}</span>
            <div className="allocation-info">
              <strong>{allocation.name}</strong>
              <span>
                {allocation.shortName} · {allocation.pcName}
              </span>
            </div>
            <div className="actions">
              <button
                type="button"
                className="ghost"
                aria-label="Move up"
                disabled={index === 0 || busy}
                onClick={() => move(index, -1)}
              >
                ↑
              </button>
              <button
                type="button"
                className="ghost"
                aria-label="Move down"
                disabled={index === allocations.length - 1 || busy}
                onClick={() => move(index, 1)}
              >
                ↓
              </button>
              <button
                type="button"
                className="danger"
                disabled={busy}
                onClick={() => removeMutation.mutate(allocation.workstationId)}
              >
                Remove
              </button>
            </div>
          </li>
        ))}
        {allocations.length === 0 && !allocationsQuery.isLoading && (
          <li className="empty">No workstations allocated yet.</li>
        )}
      </ol>
    </section>
  );
}

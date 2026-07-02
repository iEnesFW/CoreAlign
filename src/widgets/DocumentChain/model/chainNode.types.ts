export type ChainEntity = 'quote' | 'order' | 'invoice';

export type ChainNodeKind = 'quote' | 'order' | 'shipment' | 'invoice' | 'creditNote' | 'payment';

export type ChainNodeState = 'done' | 'partial' | 'pending';

export interface ChainNode {
  kind: ChainNodeKind;
  id: string;
  label: string;
  statusLabel: string;
  state: ChainNodeState;
  to: string | null;
  isCurrent: boolean;
}

import { describe, expect, it, vi, beforeAll } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Modal } from '@/shared/ui/Modal';
import i18n from '@/app/i18n';

beforeAll(async () => {
  await i18n.changeLanguage('en');
});

describe('Modal', () => {
  it('renders nothing when open is false', () => {
    render(
      <Modal open={false} onClose={() => {}} title="t">
        <p>hidden body</p>
      </Modal>,
    );
    expect(screen.queryByText('hidden body')).not.toBeInTheDocument();
  });

  it('renders title, body and close button when open', () => {
    render(
      <Modal open onClose={() => {}} title="Title" description="Desc">
        <p>body</p>
      </Modal>,
    );
    expect(screen.getByText('Title')).toBeInTheDocument();
    expect(screen.getByText('Desc')).toBeInTheDocument();
    expect(screen.getByText('body')).toBeInTheDocument();
  });

  it('renders optional footer', () => {
    render(
      <Modal open onClose={() => {}} title="t" footer={<button type="button">Save</button>}>
        x
      </Modal>,
    );
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });

  it('invokes onClose on close button click', async () => {
    const onClose = vi.fn();
    render(
      <Modal open onClose={onClose} title="t">
        x
      </Modal>,
    );
    const buttons = screen.getAllByRole('button');
    await userEvent.click(buttons[0]);
    expect(onClose).toHaveBeenCalled();
  });

  it('invokes onClose on Escape key', () => {
    const onClose = vi.fn();
    render(
      <Modal open onClose={onClose} title="t">
        x
      </Modal>,
    );
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(onClose).toHaveBeenCalled();
  });

  it('does not invoke onClose on inner click (stopPropagation)', async () => {
    const onClose = vi.fn();
    render(
      <Modal open onClose={onClose} title="t">
        <span data-testid="inner">body</span>
      </Modal>,
    );
    await userEvent.click(screen.getByTestId('inner'));
    expect(onClose).not.toHaveBeenCalled();
  });
});

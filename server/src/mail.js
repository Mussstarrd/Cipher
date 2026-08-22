/**
 * Mail, the simple way: IMAP in, SMTP out, one Gmail app password.
 *
 * No OAuth, no Google Cloud project, no verification, nothing to apply for.
 * Turn on 2FA, generate a 16-character app password, done. For one household
 * this is strictly better than OAuth — nothing expires, nothing gets revoked by
 * a policy change, and there is no consent screen to re-approve.
 */
import { ImapFlow } from "imapflow";
import nodemailer from "nodemailer";
import { simpleParser } from "mailparser";

const USER = process.env.GMAIL_USER;
const PASS = process.env.GMAIL_APP_PASSWORD;

export const mailReady = () => Boolean(USER && PASS);

/** Noise that should never reach a family brief. */
const JUNK = [
  /no[-_.]?reply@.*(kohls|walmart|amazon|adobe|linkedin|upstart|cymatics)/i,
  /(unsubscribe|newsletter|promo|deal of the day|% off)/i,
];
const isJunk = (m) =>
  JUNK.some((r) => r.test(m.from || "") || r.test(m.subject || ""));

/**
 * Everything new since the last check. Tracks the highest UID seen so a
 * restart never re-reads the inbox and never skips a message.
 */
export async function fetchNew(sinceUid = 0, max = 40) {
  if (!mailReady()) return { messages: [], lastUid: sinceUid, error: "no credentials" };
  const client = new ImapFlow({
    host: "imap.gmail.com", port: 993, secure: true,
    auth: { user: USER, pass: PASS }, logger: false,
  });
  const out = [];
  let lastUid = sinceUid;
  try {
    await client.connect();
    const lock = await client.getMailboxLock("INBOX");
    try {
      const range = sinceUid > 0 ? `${sinceUid + 1}:*` : "1:*";
      for await (const msg of client.fetch({ uid: range }, { uid: true, source: true })) {
        if (msg.uid <= sinceUid) continue;        // gmail returns the anchor
        lastUid = Math.max(lastUid, msg.uid);
        const p = await simpleParser(msg.source);
        const m = {
          uid: msg.uid,
          from: p.from?.text || "",
          subject: p.subject || "(no subject)",
          at: (p.date || new Date()).toISOString(),
          text: (p.text || "").replace(/\s+\n/g, "\n").trim().slice(0, 4000),
        };
        if (!isJunk(m)) out.push(m);
      }
    } finally { lock.release(); }
    await client.logout();
  } catch (e) {
    // Never silently return an empty inbox — that reads as "quiet day".
    return { messages: out, lastUid, error: String(e?.message || e) };
  }
  return { messages: out.slice(-max), lastUid, error: null };
}

let tx = null;
const transport = () =>
  (tx ||= nodemailer.createTransport({
    host: "smtp.gmail.com", port: 465, secure: true,
    auth: { user: USER, pass: PASS },
  }));

/** Send as the household. Callers are responsible for asking a human first. */
export async function send({ to, subject, body, replyTo }) {
  if (!mailReady()) throw new Error("mail not configured");
  return transport().sendMail({
    from: USER, to, subject, text: body,
    ...(replyTo ? { inReplyTo: replyTo, references: replyTo } : {}),
  });
}

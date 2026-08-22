/** Fire one check-in by hand, for testing: `npm run wake -- 07:00` */
import { wake } from "./brain.js";
const slot = process.argv[2] || "07:00";
console.log(await wake(slot));

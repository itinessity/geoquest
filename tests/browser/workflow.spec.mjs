import { test, expect } from '@playwright/test';
test.beforeEach(async ({page})=>{
 await page.route('https://fonts.googleapis.com/**',r=>r.fulfill({contentType:'text/css',body:''}));
 await page.route('https://tile.openstreetmap.org/**',r=>r.fulfill({contentType:'image/svg+xml',body:'<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#1d3440"/></svg>'}));
});
test('complete the main workflow',async({page})=>{
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('/');

await page.locator('#start').click();
await expect(page.locator('#round')).toHaveText('ROUND 1 / 5');
for(let round=1;round<=5;round++){
 await page.locator('#lat').fill('20');await page.locator('#lon').fill('10');await page.locator('#guess').click();
 await expect(page.locator('#next')).toBeVisible();await expect(page.locator('#guess')).toBeDisabled();
 await page.locator('#next').click();
 if(round<5) await expect(page.locator('#round')).toHaveText(`ROUND ${round+1} / 5`);
}
await expect(page.locator('#round')).toHaveText('JOURNEY COMPLETE');
await expect(page.locator('#answers tr')).toHaveCount(5);

 expect(errors).toEqual([]);
});
test('mobile layout stays within the viewport',async({page})=>{
 await page.setViewportSize({width:390,height:844});await page.goto('/');
 await expect(page.locator('h1')).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth)).toBeTruthy();
});
